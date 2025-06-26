/****************************************************************************

Copyright 2021 sophieml1989@gmail.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

****************************************************************************/


using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UBlockly
{
    public class CSharpRunner : Runner
    {
        private readonly Names mVariableNames;
        private readonly Datas mVariableDatas;
        private readonly List<CmdRunner> mCodeRunners;
        
        public CSharpRunner(Names variableNames, Datas variableDatas)
        {
            mVariableNames = variableNames;
            mVariableDatas = variableDatas;
            mCodeRunners = new List<CmdRunner>();
        }

        public override void Run(Workspace workspace)
        {
            Debug.Log("<color=cyan><b>CSharp.Runner.Run()</b></color> - Ejecución iniciada. Buscando bloques de evento...");

            mVariableNames.Reset();
            mVariableDatas.Reset();
            CurStatus = Status.Running;

            // >>>>> LÓGICA MODIFICADA PARA BUSCAR BLOQUES DE EVENTO <<<<<

            // 1. Obtener todos los bloques superiores del workspace.
            List<BlockModel> topBlocks = workspace.GetTopBlocks(true);

            // 2. Filtrarlos para encontrar solo los que son bloques de inicio (bandera verde).
            List<BlockModel> startEventBlocks = topBlocks.FindAll(block => block.Type.Equals("event_when_flag_clicked"));

            if (startEventBlocks.Count == 0)
            {
                Debug.LogWarning("CSharp.Runner: No se encontró ningún bloque 'Al hacer clic en bandera verde' para ejecutar.");
                FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop));
                return;
            }

            // 3. Crear una lista de los SCRIPTS a ejecutar (los bloques CONECTADOS a la bandera verde).
            List<BlockModel> scriptsToRun = new List<BlockModel>();
            foreach (var eventBlock in startEventBlocks)
            {
                if (eventBlock.NextBlock != null)
                {
                    scriptsToRun.Add(eventBlock.NextBlock);
                }
            }

            if (scriptsToRun.Count == 0)
            {
                Debug.Log("CSharp.Runner: Se encontraron bloques de bandera verde, pero no tienen ningún script conectado.");
                FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop));
                return;
            }

            // 4. Ejecutar la lista de scripts, NO los bloques de evento.
            if (workspace.Options.Synchronous)
            {
                RunSync(scriptsToRun);
            }
            else
            {
                RunAsync(scriptsToRun);
            }
        }

        private void RunSync(List<BlockModel> topBlocks)
        {
            foreach (BlockModel block in topBlocks)
            {
                // La lógica interna de RunSync y RunAsync no necesita cambios.
                CmdRunner runner = CmdRunner.Create(block.Type);
                mCodeRunners.Add(runner);

                runner.RunMode = RunMode;
                runner.SetFinishCallback(() =>
                {
                    GameObject.Destroy(runner.gameObject);
                    mCodeRunners.Remove(runner);
                    if (mCodeRunners.Count == 0)
                    {
                        CurStatus = Status.Stop;
                        FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop));
                    }
                });
                runner.StartRun(new CmdEnumerator(block));
            }
        }
        private void RunAsync(List<BlockModel> topBlocks)
        {
            CmdRunner runner = CmdRunner.Create(topBlocks[0].Type);
            mCodeRunners.Add(runner);

            runner.RunMode = RunMode;

            int index = 0;
            runner.SetFinishCallback(() =>
            {
                index++;
                if (index < topBlocks.Count)
                {
                    runner.StartRun(new CmdEnumerator(topBlocks[index]));
                }
                else
                {
                    GameObject.Destroy(runner.gameObject);
                    mCodeRunners.Clear();
                    CurStatus = Status.Stop;
                    FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop));
                }
            });
            runner.StartRun(new CmdEnumerator(topBlocks[0]));
        }


        public override void Pause()
        {
            if (RunMode == Mode.Step || CurStatus != Status.Running)
                return;
            CurStatus = Status.Pause;
            
            foreach (CmdRunner runner in mCodeRunners)
            {
                if (runner.CurStatus == Runner.Status.Running)
                    runner.Pause();
            }
            CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Pause));
        }

        public override void Resume()
        {
            if (RunMode == Mode.Step || CurStatus != Status.Pause)
                return;
            CurStatus = Status.Running;
            
            foreach (CmdRunner runner in mCodeRunners)
            {
                if (runner.CurStatus == Runner.Status.Pause)
                    runner.Resume();
            }
            CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Resume));
        }

        public override void Stop()
        {
            if (CurStatus == Status.Stop)
                return;
            CurStatus = Status.Stop;
            
            foreach (CmdRunner runner in mCodeRunners)
            {
                runner.Stop();
                GameObject.Destroy(runner.gameObject);
            }
            mCodeRunners.Clear();
            
            CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Stop));
        }

        public override void Error(string msg)
        {
            CurStatus = Status.Stop;
            
            foreach (CmdRunner runner in mCodeRunners)
            {
                runner.Stop();
            }
            CSharp.Runner.FireUpdate(new RunnerUpdateState(RunnerUpdateState.Error, msg));
        }

        public override void Step()
        {
            //fix bug: mCodeRunners can be modified in loop. If runner finishes running, it is removed from the list
            for (int i = mCodeRunners.Count - 1; i >= 0; i--)
            {
                mCodeRunners[i].Step();
            }
        }

        public List<string> GetCallStack()
        {
            if (RunMode != Mode.Step || mCodeRunners.Count == 0)
                return null;

            List<string> callstack = mCodeRunners[0].GetCallStack();
            for (int i = 1; i < mCodeRunners.Count; i++)
            {
                callstack.Add("");
                callstack.Concat(mCodeRunners[i].GetCallStack());
            }
            return callstack;
        }
    }
}