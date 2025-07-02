using System.Collections;
using UnityEngine;

namespace UBlockly
{
    // Vinculamos este intérprete al tipo de bloque que definimos en el JSON.
    [CodeInterpreter(BlockType = "motion_move_steps")]
    public class Motion_Move_Steps_Cmdtor : EnumeratorCmdtor
    {
        // Heredamos de EnumeratorCmdtor para que la ejecución se haga en una corrutina.
        protected override IEnumerator Execute(BlockModel block)
        {
            // 1. Obtener el valor del campo "STEPS" del bloque.
           
            float steps = float.Parse(block.GetFieldValue("STEPS"));

            IEnumerator moveCoroutine = GameAPI.MoveRobotSteps(steps);

            // 2. Llamar a nuestro puente GameAPI.
           // GameAPI.MoveRobotSteps(steps);

            // 3. Ceder el control por un frame.
           
          //  yield return null;
            while (moveCoroutine.MoveNext())
            {
                yield return moveCoroutine.Current;
            }

            Debug.Log("<color=lime><b>Cmdtor de Movimiento:</b></color> La corrutina de movimiento manual ha terminado.");
        }
    }

    [CodeInterpreter(BlockType = "motion_turn_right")]
    public class Motion_Turn_Right_Cmdtor : EnumeratorCmdtor
    {
        protected override IEnumerator Execute(BlockModel block)
        {
            float degrees = float.Parse(block.GetFieldValue("DEGREES"));

            // Llamamos a la misma función, pero con un valor positivo para los grados.
            IEnumerator turnCoroutine = GameAPI.TurnRobot(degrees);
            while (turnCoroutine.MoveNext())
            {
                yield return turnCoroutine.Current;
            }
        }
    }

    [CodeInterpreter(BlockType = "motion_turn_left")]
    public class Motion_Turn_Left_Cmdtor : EnumeratorCmdtor
    {
        protected override IEnumerator Execute(BlockModel block)
        {
            float degrees = float.Parse(block.GetFieldValue("DEGREES"));

            // ¡La única diferencia! Pasamos los grados como un número negativo para girar a la izquierda.
            IEnumerator turnCoroutine = GameAPI.TurnRobot(-degrees);
            while (turnCoroutine.MoveNext())
            {
                yield return turnCoroutine.Current;
            }
        }
    }


}