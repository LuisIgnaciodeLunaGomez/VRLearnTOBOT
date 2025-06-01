using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.TestTools;
using static UnityEngine.EventSystems.StandaloneInputModule;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class WorkspaceCoreTests
{
    private WorkSpaceModel workspace;

    [SetUp] // Se ejecuta ANTES de CADA test en esta clase
    public void Setup()
    {
       
        BlockDefinition.LoadAllDefinitionsFromXml(); 

        WorkSpaceModel.WorkspaceOptions options = new WorkSpaceModel.WorkspaceOptions();
        workspace = new WorkSpaceModel(options, "TestWS_" + System.Guid.NewGuid().ToString());
    }

    [TearDown] // Se ejecuta DESPUÉS de CADA test
    public void Teardown()
    {
        if (workspace != null)
        {
            workspace.Dispose(); // Limpia el workspace para el siguiente test
            workspace = null;
        }
        BlockFactory.Instance.Clear(); // Limpia el BlockFactory
    }

    [Test]
    public void Workspace_NewBlock_CreatesBlockAndRegistersInDBAndTopBlocks()
    {
        // Arrange
        string blockType = "motion_movesteps"; // Un tipo de bloque válido
        string blockId = "block_123";

        // Act
        BlockModel block = workspace.NewBlock(blockType, blockId);

        // Assert
        Assert.IsNotNull(block, "El bloque no debería ser nulo.");
        Assert.AreEqual(blockType, block.Type, "El tipo de bloque no coincide.");
        Assert.AreEqual(blockId, block.ID, "El ID del bloque no coincide.");
        Assert.AreEqual(workspace, block.Workspace, "El workspace del bloque no es el correcto.");
        Assert.IsTrue(workspace.BlockDB.ContainsKey(blockId), "El bloque no fue añadido a BlockDB.");
        Assert.IsTrue(workspace.BlockDB[blockId] == block, "El bloque en BlockDB no es el mismo objeto.");
        Assert.IsTrue(workspace.TopBlocks.Contains(block), "El bloque no fue añadido a TopBlocks (debería si no tiene padre).");
    }

    [Test]
    public void BlockModel_SetParent_UpdatesHierarchyAndTopBlocks()
    {
        // Arrange
        BlockModel parentBlock = workspace.NewBlock("event_whenflagclicked", "parent1");
        BlockModel childBlock = workspace.NewBlock("motion_movesteps", "child1");

        // Asegurarse de que inicialmente el hijo es un top block
        Assert.IsTrue(workspace.TopBlocks.Contains(childBlock), "Child block debería ser TopBlock inicialmente.");

        // Act
        
        parentBlock.NextConnection.Connect(childBlock.PreviousConnection);

        // Assert
        Assert.AreEqual(parentBlock, childBlock.ParentBlock, "ParentBlock del hijo no es el correcto.");
        Assert.IsTrue(parentBlock.ChildBlocks.Contains(childBlock), "El hijo no está en la lista ChildBlocks del padre.");
        Assert.IsFalse(workspace.TopBlocks.Contains(childBlock), "Child block no debería ser TopBlock después de conectar.");
        Assert.IsTrue(workspace.TopBlocks.Contains(parentBlock), "Parent block debería seguir siendo TopBlock.");
    }

    [Test]
    public void BlockModel_UnPlug_RemovesParentAndMakesTopBlock()
    {
        // Arrange
        BlockModel blockA = workspace.NewBlock("event_whenflagclicked", "blockA");
        BlockModel blockB = workspace.NewBlock("motion_movesteps", "blockB");
        BlockModel blockC = workspace.NewBlock("motion_movesteps", "blockC");

        blockA.NextConnection.Connect(blockB.PreviousConnection);
        blockB.NextConnection.Connect(blockC.PreviousConnection);

        Assert.AreEqual(blockA, blockB.ParentBlock);
        Assert.AreEqual(blockB, blockC.ParentBlock);
        Assert.IsFalse(workspace.TopBlocks.Contains(blockB));

        // Act
        blockB.UnPlug(true); // Heal stack

        // Assert
        Assert.IsNull(blockB.ParentBlock, "blockB no debería tener ParentBlock después de UnPlug.");
        Assert.IsTrue(workspace.TopBlocks.Contains(blockB), "blockB debería ser TopBlock después de UnPlug.");
        Assert.AreEqual(blockC, blockA.NextBlock, "blockA.NextBlock debería ser blockC después de heal stack.");
        Assert.AreEqual(blockA, blockC.ParentBlock, "ParentBlock de blockC debería ser blockA después de heal stack.");
    }

    [Test]
    public void BlockModel_GetConnections_ReturnsAllConnections()
    {
        // Arrange
        BlockModel block = workspace.NewBlock("motion_movesteps", "block1"); // Este tiene prev, next, y un input "STEPS"

        // Act
        List<ConnectionModel> connections = block.GetConnections();

        // Assert
     
        int expectedConnections = 3;
        if (block.OutputConnection != null) expectedConnections++; // Si fuera un bloque de output

        Assert.AreEqual(expectedConnections, connections.Count, "Número incorrecto de conexiones encontradas.");
        Assert.IsTrue(connections.Contains(block.PreviousConnection), "Falta PreviousConnection.");
        Assert.IsTrue(connections.Contains(block.NextConnection), "Falta NextConnection.");

        InputModel stepsInput = block.GetInput("STEPS");
        if (stepsInput != null && stepsInput.Connection != null)
        {
            Assert.IsTrue(connections.Contains(stepsInput.Connection), "Falta la conexión del Input STEPS.");
        }
        else
        {
           
            if (expectedConnections == 3) Assert.Fail("Se esperaban 3 conexiones incluyendo STEPS, pero el input STEPS no tiene conexión o no existe.");
        }
    }

    [Test]
    public void BlockModel_GetChildren_And_NextBlock_And_RootBlock_WorkCorrectly()
    {
        // Arrange
        BlockModel top = workspace.NewBlock("event_whenflagclicked", "top");
        BlockModel middle = workspace.NewBlock("motion_movesteps", "middle");
        BlockModel bottom = workspace.NewBlock("motion_movesteps", "bottom");

        
        top.NextConnection.Connect(middle.PreviousConnection);
        middle.NextConnection.Connect(bottom.PreviousConnection);

        // Act & Assert
        Assert.AreEqual(middle, top.NextBlock, "NextBlock de top debería ser middle.");
        Assert.IsNull(bottom.NextBlock, "NextBlock de bottom (último) debería ser null.");

        Assert.AreEqual(1, top.ChildBlocks.Count, "Top debería tener 1 ChildBlock (middle)."); // Asumiendo que Next es un Child
        Assert.IsTrue(top.ChildBlocks.Contains(middle), "Middle debería ser ChildBlock de top.");
        Assert.AreEqual(1, middle.ChildBlocks.Count, "Middle debería tener 1 ChildBlock (bottom).");
        Assert.IsTrue(middle.ChildBlocks.Contains(bottom), "Bottom debería ser ChildBlock de middle.");
        Assert.AreEqual(0, bottom.ChildBlocks.Count, "Bottom no debería tener ChildBlocks.");

        Assert.AreEqual(top, top.RootBlock, "RootBlock de top debería ser él mismo.");
        Assert.AreEqual(top, middle.RootBlock, "RootBlock de middle debería ser top.");
        Assert.AreEqual(top, bottom.RootBlock, "RootBlock de bottom debería ser top.");
    }

    [Test]
    public void Workspace_XmlSerialization_BlockToDomAndDomToBlockHeadless_PreservesData()
    {
        // Arrange
        BlockModel originalBlock = workspace.NewBlock("motion_movesteps", "original_id");
        originalBlock.SetFieldValue("STEPS", "25"); 
        InputModel stepsInput = originalBlock.GetInput("STEPS");
        if (stepsInput != null && stepsInput.FieldRow.Count > 0 && stepsInput.FieldRow[0] is FieldNumberInputModel numField)
        {
            numField.SetValue("25"); 
        }
        else if (stepsInput != null && stepsInput.FieldRow.Count > 0 && stepsInput.FieldRow[0] is FieldTextInputModel txtField)
        {
           
            txtField.SetValue("25");
        }
        else
        {
            Assert.Fail("No se pudo encontrar o establecer el campo 'STEPS' en el bloque original.");
        }

        originalBlock.XY = new Vector2(100, 200);
        originalBlock.Collapsed = true;

        // Act
  
    
        XmlNode xmlNode = Xml.BlockToDom(originalBlock, false); // optNoId = false para incluir ID

        // Crear un nuevo workspace para asegurar aislamiento en la deserialización
        WorkSpaceModel newWorkspace = new WorkSpaceModel(new WorkSpaceModel.WorkspaceOptions(), "deserializeWS");
        BlockModel deserializedBlock = Xml.DomToBlockHeadless(xmlNode, newWorkspace);
        newWorkspace.AddTopBlock(deserializedBlock);

        // Assert
        Assert.IsNotNull(deserializedBlock, "Bloque deserializado no debería ser nulo.");
        Assert.AreEqual(originalBlock.ID, deserializedBlock.ID, "ID no coincide."); 
        Assert.AreEqual(originalBlock.Type, deserializedBlock.Type, "Tipo no coincide.");

        string originalFieldValue = "Error";
        InputModel originalStepsInput = originalBlock.GetInput("STEPS");
        if (originalStepsInput != null && originalStepsInput.FieldRow.Count > 0) originalFieldValue = originalStepsInput.FieldRow[0].GetValue();

        string deserializedFieldValue = "Error";
        InputModel deserializedStepsInput = deserializedBlock.GetInput("STEPS");
        if (deserializedStepsInput != null && deserializedStepsInput.FieldRow.Count > 0) deserializedFieldValue = deserializedStepsInput.FieldRow[0].GetValue();

        Assert.AreEqual(originalFieldValue, deserializedFieldValue, "Valor del campo 'STEPS' no coincide.");

        Assert.AreEqual(originalBlock.Collapsed, deserializedBlock.Collapsed, "Estado 'Collapsed' no coincide.");
    
        newWorkspace.Dispose();
    }
}