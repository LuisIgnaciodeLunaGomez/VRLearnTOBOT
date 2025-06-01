using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass()]
    public class BlockModelTests
    {
        private WorkSpaceModel workspace;
        private string defaultBlockType = "motion_movesteps"; // Un tipo de bloque común para tests

        // Método para cargar definiciones de bloques de forma controlada para los tests
        
        private void InitializeBlockDefinitionsForTesting()
        {
       
            try
            {
            
                BlockDefinition.LoadAllDefinitionsFromXml(/* "ruta/a/xml/de/test" */);
            }
            catch (System.Exception ex)
            {
                
                System.Diagnostics.Debug.WriteLine($"ADVERTENCIA: Falló la carga de definiciones de bloques para testing: {ex.Message}");
            }
        }

        [TestInitialize] 
        public void Setup()
        {
            InitializeBlockDefinitionsForTesting();

            workspace = new WorkSpaceModel(new WorkSpaceModel.WorkspaceOptions(), "TestWS_BlockModel");
        }

        [TestCleanup] 
        public void Teardown()
        {
            workspace?.Dispose(); 
            BlockFactory.Instance.Clear(); 
        }

        [TestMethod()]
        public void BlockModel_Constructor_InitializesCorrectly()
        {
            // Arrange
            string type = "test_type";
            string id = "test_id_123";

            // Act
            BlockModel block = new BlockModel(workspace, type, id);

            // Assert
            Assert.AreEqual(type, block.Type, "El tipo no se inicializó correctamente.");
            Assert.AreEqual(id, block.ID, "El ID no se inicializó correctamente.");
            Assert.AreEqual(workspace, block.Workspace, "El Workspace no se asignó correctamente.");
            Assert.IsNotNull(block.InputList, "InputList no debería ser nulo.");
            Assert.AreEqual(0, block.InputList.Count, "InputList debería estar vacío inicialmente (antes de Reshape).");
            Assert.IsNotNull(block.ChildBlocks, "ChildBlocks no debería ser nulo.");
            Assert.IsTrue(workspace.BlockDB.ContainsKey(id), "El bloque no se registró en el BlockDB del workspace.");
            Assert.IsTrue(workspace.TopBlocks.Contains(block), "Un nuevo bloque debería ser un TopBlock.");
        }

        [TestMethod()]
        public void BlockModel_SetParent_UpdatesHierarchyAndRemovesFromTopBlocks()
        {
            // Arrange
            BlockModel parentBlock = new BlockModel(workspace, "parent_type", "parent_id");
            BlockModel childBlock = new BlockModel(workspace, "child_type", "child_id");

            // Act
            childBlock.SetParent(parentBlock);

            // Assert
            Assert.AreEqual(parentBlock, childBlock.ParentBlock, "ParentBlock no se estableció correctamente.");
            Assert.IsTrue(parentBlock.ChildBlocks.Contains(childBlock), "El bloque hijo no se añadió a ChildBlocks del padre.");
            Assert.IsFalse(workspace.TopBlocks.Contains(childBlock), "El bloque hijo no debería estar en TopBlocks después de tener un padre.");
        }

        [TestMethod()]
        public void BlockModel_SetParent_ToNull_MakesItTopBlock()
        {
            // Arrange
            BlockModel parentBlock = new BlockModel(workspace, "parent_type_2", "parent_id_2");
            BlockModel childBlock = new BlockModel(workspace, "child_type_2", "child_id_2");
            childBlock.SetParent(parentBlock); // childBlock ahora tiene un padre

            // Act
            childBlock.SetParent(null); // Hacerlo huérfano

            // Assert
            Assert.IsNull(childBlock.ParentBlock, "ParentBlock debería ser nulo.");
            Assert.IsFalse(parentBlock.ChildBlocks.Contains(childBlock), "El bloque hijo debería haber sido removido de ChildBlocks del antiguo padre.");
            Assert.IsTrue(workspace.TopBlocks.Contains(childBlock), "El bloque hijo debería ser un TopBlock ahora.");
        }

        [TestMethod()]
        public void BlockModel_UnPlug_FromPreviousConnection_HealsStackAndUpdatesParent()
        {
            // Arrange
            BlockModel blockA = workspace.NewBlock("event_whenflagclicked", "A"); // Tiene Next
            BlockModel blockB = workspace.NewBlock(defaultBlockType, "B");      // Tiene Prev y Next
            BlockModel blockC = workspace.NewBlock(defaultBlockType, "C");      // Tiene Prev

            blockA.NextConnection.Connect(blockB.PreviousConnection);
            blockB.NextConnection.Connect(blockC.PreviousConnection);

            Assert.AreEqual(blockA, blockB.ParentBlock);
            Assert.AreEqual(blockB, blockC.ParentBlock);

            // Act
            blockB.UnPlug(true); // Unplug B, con heal stack

            // Assert
            Assert.IsNull(blockB.ParentBlock, "BlockB no debería tener padre.");
            Assert.IsTrue(workspace.TopBlocks.Contains(blockB), "BlockB debería ser TopBlock.");
            Assert.AreEqual(blockC, blockA.NextBlock, "A debería estar conectado a C.");
            Assert.AreEqual(blockA, blockC.ParentBlock, "El padre de C debería ser A.");
            Assert.IsFalse(blockB.NextConnection.IsConnected, "El Next de B debería estar desconectado.");
            Assert.IsFalse(blockB.PreviousConnection.IsConnected, "El Prev de B debería estar desconectado.");
        }

        [TestMethod()]
        public void BlockModel_GetConnections_ReturnsCorrectConnections()
        {
            // Arrange
            BlockModel block = workspace.NewBlock("motion_movesteps", "move_conn_test");
         

            // Act
            List<ConnectionModel> connections = block.GetConnections();

            // Assert
            Assert.IsNotNull(connections, "La lista de conexiones no debe ser nula.");
           
            int expectedCount = 2; // Prev, Next
            if (block.InputList.Any(input => input.Name == "STEPS" && input.Connection != null))
            {
                expectedCount++;
            }
         

            Assert.AreEqual(expectedCount, connections.Count, $"Número de conexiones incorrecto para {block.Type}.");

            Assert.IsTrue(connections.Any(c => c == block.PreviousConnection), "PreviousConnection falta en la lista.");
            Assert.IsTrue(connections.Any(c => c == block.NextConnection), "NextConnection falta en la lista.");

            InputModel stepsInput = block.GetInput("STEPS");
            if (stepsInput != null && stepsInput.Connection != null)
            {
                Assert.IsTrue(connections.Any(c => c == stepsInput.Connection), "La conexión del Input 'STEPS' falta en la lista.");
            }
        }

        [TestMethod()]
        public void BlockModel_GetInput_And_HasInput_Work()
        {
            // Arrange
            BlockModel block = workspace.NewBlock("motion_movesteps", "getInputTest");

            // Act & Assert
            Assert.IsTrue(block.HasInput("STEPS"), "Debería tener un input llamado 'STEPS'.");
            Assert.IsFalse(block.HasInput("NON_EXISTENT_INPUT"), "No debería tener un input inexistente.");

            InputModel stepsInput = block.GetInput("STEPS");
            Assert.IsNotNull(stepsInput, "GetInput('STEPS') no debería devolver nulo.");
            Assert.AreEqual("STEPS", stepsInput.Name);

            InputModel nullInput = block.GetInput("NON_EXISTENT_INPUT");
            Assert.IsNull(nullInput, "GetInput para un input inexistente debería devolver nulo.");
        }

        [TestMethod()]
        public void BlockModel_SetFieldValue_UpdatesField()
        {
            // Arrange
            BlockModel block = workspace.NewBlock("motion_movesteps", "setFieldTest");
            string fieldName = "STEPS"; 
            InputModel stepsInput = block.GetInput("STEPS");
            Assert.IsNotNull(stepsInput, "El bloque debe tener el input 'STEPS'.");
            Assert.IsTrue(stepsInput.FieldRow.Count > 0, "El input 'STEPS' debe tener al menos un Field.");

            string actualFieldNameInInput = stepsInput.FieldRow[0].Name; 

            // Act
            string newValue = "50";
            block.SetFieldValue(actualFieldNameInInput, newValue);

            // Assert
            Assert.AreEqual(newValue, block.GetFieldValue(actualFieldNameInInput));
        }
    }
}