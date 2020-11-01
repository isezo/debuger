using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KizhiPart3
{
    public class SaveClass
    {
        public string defName;
        public string saveCom;
        public string saveVar;
        public int saveVal;
        public int markLine;
    }

    public class StackSave
    {
        public int index;
        public string defName;
    }

    public class Debugger
    {
        public enum EnumCom
        {
            command = 0,
            name = 1,
            variable = 1,
            value = 2
        }
        public int saveMarkLine, firstCallDef, currentLine, lastBreak;
        public bool setCode, run, checkBreak, breakIgnor, stepOverIgnor = false;
        public bool start = true;
        public string saveDefName;
        private TextWriter _writer;
        public string command;
        public Dictionary<string, int> dictionary = new Dictionary<string, int>();
        public Dictionary<string, int> dicLine = new Dictionary<string, int>();
        public List<int> breakLine = new List<int>();
        public List<SaveClass> SaveDefList = new List<SaveClass>();
        public List<SaveClass> SaveComList = new List<SaveClass>();
        public Stack<StackSave> StackSaves = new Stack<StackSave>();

        public Debugger(TextWriter writer)
        {
            _writer = writer;
        }

        public static void Main(string[] args)
        {
            Debugger inter = new Debugger(Console.Out);
            do
            {
                inter.command = Console.ReadLine();
                inter.ExecuteLine(inter.command);
            } while (string.Compare(inter.command, "exit") != 0);
        }

        public void ResetMemory()
        {
            dictionary.Clear();
            breakLine.Clear();
            saveMarkLine = 0;
            firstCallDef = 0;
            currentLine = 0;
            lastBreak = 0;
            checkBreak = false;
            breakIgnor = false;
            run = false;
            start = true;
        }

        public void ExecuteLine(string command)
        {
            if (command.StartsWith("add break"))
            {
                int tempBreak = Int32.Parse(command.Remove(0, 10));
                var res = breakLine
                    .Where(e => e == tempBreak)
                    .ToList();
                if (res.Count == 0)
                {
                    breakLine.Add(tempBreak);
                    for (int i = 0; i < breakLine.Count - 1; i++)
                    {
                        for (int j = i + 1; j < breakLine.Count; j++)
                        {
                            if (breakLine[i] > breakLine[j])
                            {
                                int temp = breakLine[i];
                                breakLine[i] = breakLine[j];
                                breakLine[j] = temp;
                            }
                        }
                    }
                }
                else _writer.WriteLine("Точка остановки уже существует");
            }

            if (setCode)
            {
                if (command == "end set code")
                {
                    setCode = false;
                }
                else ExecCommand(command);
            }

            if (!setCode)
            {

                if (command == "set code")
                {
                    dictionary.Clear();
                    SaveDefList.Clear();
                    SaveComList.Clear();
                    setCode = true;
                }
                else
                    SwithDebugOrRun(command);
            }
        }

        public void ExecCommand(string command)
        {
            string[] str_split = command.Split(new string[] { "\\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < str_split.Length; i++)
                SwithInterpreter(str_split[i]);
        }

        public void SwithDebugOrRun(string command)
        {
            if (command == "step over")
            {
                if (run)
                {
                    //while (true)
                    //{
                    stepOverIgnor = true;
                    string temp = "";
                        var lastLine = SaveDefList
                            .Where(s => s.markLine == lastBreak)
                            .ToList();
                        if (lastLine.Count == 0)
                        {
                            lastLine = SaveComList
                               .Where(s => s.markLine == lastBreak)
                               .ToList();
                            if (lastLine.Count != 0)
                            {
                                if (lastLine[0].saveCom == "call")
                                {
                                    var templ = SaveDefList
                                    .Where(s => s.defName == lastLine[0].saveVar)
                                    .ToList();
                                    if (templ.Count != 0)
                                    {
                                        lastBreak = templ[0].markLine;
                                        firstCallDef = lastLine[0].markLine;
                                        StackSave tempSaveStack = new StackSave
                                        {
                                            index = lastLine[0].markLine,
                                            defName = lastLine[0].saveVar
                                        };
                                        StackSaves.Push(tempSaveStack);
                                    }
                                }
                                temp = string.Format("{0} {1} {2}", lastLine[0].saveCom, lastLine[0].saveVar, lastLine[0].saveVal);
                                firstCallDef = lastLine[0].markLine;
                                stepOverIgnor = true;
                                SwithInterpreter(temp);
                                LastEditVar(lastLine[0]);
                                SearchLastBreak(SaveComList, lastLine[0].markLine);
                            }
                            else
                            {
                                ResetMemory();
                                start = true;
                            }
                        }
                        else
                        {
                            breakIgnor = true;
                            CallFunc(lastLine[0].markLine);
                        }
                        if (lastLine[0].markLine + 1 == lastBreak)
                        {
                        }
                    //}
                    stepOverIgnor = false;
                }
            }

            if (command == "step")
            {
                if (run)
                {
                    breakIgnor = true;
                    string temp = "";
                    var lastLine = SaveDefList
                        .Where(s => s.markLine == lastBreak)
                        .ToList();
                    if (lastLine.Count == 0)
                    {
                        lastLine = SaveComList
                           .Where(s => s.markLine == lastBreak)
                           .ToList();
                        if (lastLine.Count != 0)
                        {
                            if (lastLine[0].saveCom == "call")
                            {
                                var templ = SaveDefList
                                    .Where(s => s.defName == lastLine[0].saveVar)
                                    .ToList();
                                if (templ.Count != 0)
                                {
                                    lastBreak = templ[0].markLine;
                                    firstCallDef = lastLine[0].markLine;
                                    StackSave tempSaveStack = new StackSave
                                    {
                                        index = lastLine[0].markLine,
                                        defName = lastLine[0].saveVar
                                    };
                                    StackSaves.Push(tempSaveStack);
                                }
                                else
                                {
                                    _writer.WriteLine("Переменная отсутсвует в памяти");
                                    SearchLastBreak(SaveComList, lastLine[0].markLine);
                                }
                                
                            }
                            else
                            {
                                temp = string.Format("{0} {1} {2}", lastLine[0].saveCom, lastLine[0].saveVar, lastLine[0].saveVal);
                                SwithInterpreter(temp);
                                LastEditVar(lastLine[0]);
                                SearchLastBreak(SaveComList, lastLine[0].markLine);
                                if (lastBreak == lastLine[0].markLine)
                                {
                                    ResetMemory();                                    
                                }
                            }
                        }
                        else
                        {
                            ResetMemory();
                        }
                    }
                    else
                    {
                        CallFunc(lastLine[0].markLine);
                    }
                }
            }

            if (command == "print mem")
            {
                foreach (var e in dictionary)
                {
                    if (dicLine.ContainsKey(e.Key))
                    {
                        _writer.WriteLine("{0} {1} {2}", e.Key, e.Value, dicLine[e.Key]);
                    }
                }
            }

            if (command == "print trace")
            {
                var printStack = StackSaves.ToArray();
                for (int i = 0; i < printStack.Length; i++)
                {
                    _writer.WriteLine("{0} {1} ", printStack[i].index, printStack[i].defName);
                }
            }

            if (command == "run" && SaveComList.Count > 0)
            {
                run = true;
                if (start)
                {

                    foreach (var e in SaveComList)
                    {
                        checkBreak = true;
                        start = false;
                        var res = breakLine
                            .Where(s => s == e.markLine)
                            .ToList();
                        if (res.Count == 0)
                        {
                            if (e.saveCom == "call")
                            {
                                var firstLineNewDef = SaveDefList
                                .Where(s => s.defName == e.saveVar)
                                .ToList();
                                if (firstLineNewDef.Count != 0)
                                {
                                    firstCallDef = e.markLine;
                                    StackSave tempSaveStack = new StackSave
                                    {
                                        index = e.markLine,
                                        defName = e.saveVar
                                    };
                                    StackSaves.Push(tempSaveStack);
                                }
                            }
                            string temp = string.Format("{0} {1} {2}", e.saveCom, e.saveVar, e.saveVal);
                            SwithInterpreter(temp);
                            LastEditVar(e);
                            if (checkBreak)
                                break;
                        }
                        else
                        {
                            lastBreak = e.markLine;
                            break;
                        }
                        start = true;
                    }
                    if (!checkBreak)
                    {
                        ResetMemory();
                    }
                }
                else
                {
                    while (true)
                    {
                        string temp = "";
                        var lastLine = SaveDefList
                            .Where(s => s.markLine == lastBreak)
                            .ToList();
                        if (lastLine.Count == 0)
                        {
                            lastLine = SaveComList
                               .Where(s => s.markLine == lastBreak)
                               .ToList();
                            if (lastLine.Count != 0)
                            {
                                if (lastLine[0].saveCom == "call")
                                {
                                    var firstLineNewDef = SaveDefList
                                    .Where(s => s.defName == lastLine[0].saveVar)
                                    .ToList();
                                    if (firstLineNewDef.Count == 0)
                                    {
                                        //_writer.WriteLine("Переменная отсутсвует в памяти");
                                    }
                                    else
                                    {
                                        firstCallDef = lastLine[0].markLine;
                                        StackSave tempSaveStack = new StackSave
                                        {
                                            index = lastLine[0].markLine,
                                            defName = lastLine[0].saveVar
                                        };
                                        StackSaves.Push(tempSaveStack);
                                    }
                                }
                                temp = string.Format("{0} {1} {2}", lastLine[0].saveCom, lastLine[0].saveVar, lastLine[0].saveVal);
                                SwithInterpreter(temp);
                                LastEditVar(lastLine[0]);
                                SearchLastBreak(SaveComList, lastLine[0].markLine);
                                if(lastBreak == lastLine[0].markLine)
                                {
                                    lastBreak++;
                                }
                            }
                            else
                            {
                                ResetMemory();
                                break;
                            }
                        }
                        else
                        {
                            breakIgnor = true;
                            CallFunc(lastLine[0].markLine);
                        }
                        var res = breakLine
                            .Where(s => s == lastBreak)
                            .ToList();
                        if (res.Count != 0)
                        {
                            break;
                        }
                    }
                }
            }
        }

        private void LastEditVar(SaveClass e)
        {
            if (e.saveCom == "set" || e.saveCom == "sub")
            {
                if (dictionary.ContainsKey(e.saveVar))
                {
                    if (dicLine.ContainsKey(e.saveVar))
                        dicLine[e.saveVar] = e.markLine;
                    else
                        dicLine.Add(e.saveVar, e.markLine);
                }
            }
        }

        public void SwithInterpreter(string line)
        {
            if (!line.Contains("    "))
                saveDefName = null;

            string[] command_split = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            switch (command_split[(int)EnumCom.command])
            {
                case "set":
                case "sub":
                case "print":
                case "rem":
                    CaseFunk(command_split);
                    checkBreak = false;
                    break;

                case "def":
                    if (!run)
                    {
                        //saveMarkLine++;
                        if (SaveDefList.Count > 0)
                        {
                            foreach (var e in SaveDefList)
                            {
                                if (e.defName == command_split[(int)EnumCom.name])
                                {
                                    _writer.WriteLine("Функция {0} уже существует", command_split[(int)EnumCom.name]);
                                    saveDefName = null;
                                    break;
                                }
                                else
                                {
                                    saveDefName = command_split[(int)EnumCom.name];
                                }
                            }
                            if (saveDefName != null)
                                SaveFunke(command_split, true);
                        }
                        else
                        {
                            saveDefName = command_split[(int)EnumCom.name];
                            SaveFunke(command_split, true);
                        }
                    }
                    break;

                case "call":
                    if (saveDefName != null)
                    {
                        SaveFunke(command_split, true);
                    }
                    else
                    {
                        if (run)
                        {
                            var result = SaveDefList
                                    .Where(z => z.defName == command_split[(int)EnumCom.name])
                                    .ToList();
                            if (result.Count != 0)
                                CallFunc(result[0].markLine);
                            else
                            {
                                _writer.WriteLine("Переменная отсутствует в памяти");
                                checkBreak = false;
                            }
                        }
                        else
                            SaveFunke(command_split, false);
                    }
                    break;

                default:
                    _writer.WriteLine("Неизвестная команда");
                    break;
            }
        }

        public void CaseFunk(string[] command_split)
        {
            if (saveDefName != null)
            {
                SaveFunke(command_split, true);
            }
            else
            {
                if (run)
                {
                    if (command_split.Length == 3)
                    {
                        if (Int32.TryParse(command_split[(int)EnumCom.value], out int intValue))
                        {
                            if (command_split[(int)EnumCom.command] == "set")
                                dictionary[command_split[(int)EnumCom.variable]] = intValue;

                            if (dictionary.ContainsKey(command_split[(int)EnumCom.variable]))
                            {
                                if (command_split[(int)EnumCom.command] == "sub")
                                    dictionary[command_split[(int)EnumCom.variable]] -= intValue;
                                if (command_split[(int)EnumCom.command] == "print")
                                    _writer.WriteLine(dictionary[command_split[(int)EnumCom.variable]]);
                                if (command_split[(int)EnumCom.command] == "rem")
                                    dictionary.Remove(command_split[(int)EnumCom.variable]);
                            }
                            else
                                _writer.WriteLine("Переменная отсутствует в памяти");
                        }
                    }
                }
                else
                {
                    SaveFunke(command_split, false);
                }
            }
        }

        public void SaveFunke(string[] command_split, bool SaveComOrDef)
        {
            SaveClass tempSaveFunc = new SaveClass
            {
                defName = saveDefName,
                saveCom = command_split[(int)EnumCom.command],
                saveVar = command_split[(int)EnumCom.variable],
                markLine = saveMarkLine
            };
            saveMarkLine++;
            if (command_split.Length == 3)
            {
                tempSaveFunc.saveVal = Int32.Parse(command_split[(int)EnumCom.value]);
            }
            if (SaveComOrDef)
                SaveDefList.Add(tempSaveFunc);
            else
                SaveComList.Add(tempSaveFunc);
        }


        public void SearchLastBreak(List<SaveClass> e, int index)
        {
            for (int i = 0; i < e.Count; i++)
            {
                if (e[i].markLine == index && i + 1 < e.Count)
                {
                    lastBreak = e[i + 1].markLine;
                    break;
                }
            }
        }

        public void CallFunc(int mark)
        {
            checkBreak = false;
            while (true)
            {
                var e = SaveDefList
                     .Where(s => s.markLine == mark)
                     .ToList();
                string defName = e[0].defName;
                List<int> res = new List<int>();
                if (!stepOverIgnor)
                {
                    res = breakLine
                            .Where(s => s == e[0].markLine)
                            .ToList();
                }
                if (res.Count != 0 && !breakIgnor)
                {
                    lastBreak = e[0].markLine;
                    checkBreak = true;
                    break;
                }
                else
                {
                    
                    if (e[0].saveCom == "call")
                    {
                        var firstLineNewDef = SaveDefList
                            .Where(s => s.defName == e[0].saveVar)
                            .ToList();
                        defName = e[0].saveVar;
                        if (firstLineNewDef.Count != 0)
                        {
                            StackSave tempSaveStack = new StackSave
                            {
                                index = mark,
                                defName = defName
                            };
                            StackSaves.Push(tempSaveStack);
                            mark = firstLineNewDef[0].markLine;
                            e[0] = firstLineNewDef[0];
                        }
                        else
                        {
                            _writer.WriteLine("Переменная отсутствует в памяти");
                            mark++;
                            var checkNext = SaveDefList
                                .Where(s => s.markLine == mark)
                                .ToList();
                            if (checkNext.Count == 1)
                            {
                                if (checkNext[0].defName != defName)
                                {
                                    var result = StackSaves.Pop();
                                    if (StackSaves.Count > 0)
                                    {
                                        SearchLastBreak(SaveDefList, result.index);                                        
                                    }
                                    else
                                    {
                                        SearchLastBreak(SaveComList, result.index);
                                    }                                    
                                }                                
                            }
                        }
                    }
                    if (e[0].defName == defName)
                    {
                        string temp = string.Format("{0} {1} {2}", e[0].saveCom, e[0].saveVar, e[0].saveVal);
                        SwithInterpreter(temp);
                        LastEditVar(e[0]);
                    }

                    for (int i = 0; i < SaveDefList.Count; i++)
                    {
                        if (SaveDefList[i].markLine == e[0].markLine && i + 1 < SaveDefList.Count)
                        {
                            mark = SaveDefList[i + 1].markLine;
                            break;
                        }
                    }
                    if(mark == e[0].markLine)
                    {
                        mark++;
                    }

                    var checkNextLine = SaveDefList
                        .Where(s => s.markLine == mark)
                        .ToList();
                    if (checkNextLine.Count == 1)
                    {
                        if (checkNextLine[0].defName != defName)
                        {
                            var result = StackSaves.Pop();
                            mark = result.index;
                            defName = result.defName;
                            if (StackSaves.Count == 0)
                                break;
                        }
                        if (breakIgnor)
                        {
                            lastBreak = checkNextLine[0].markLine;
                            breakIgnor = false;
                            break;
                        }
                    }
                    else
                    {
                        var mark2 = mark;
                        while (mark == mark2)
                        {
                            if (StackSaves.Count > 1)
                            {
                                var result = StackSaves.Pop();
                                mark = result.index;
                                mark2 = result.index;
                                defName = result.defName;
                                for (int i = 0; i < SaveDefList.Count; i++)
                                {
                                    if (SaveDefList[i].markLine == e[0].markLine && i + 1 < SaveDefList.Count)
                                    {
                                        mark = SaveDefList[i + 1].markLine;
                                        break;
                                    }
                                }
                            } else if (StackSaves.Count == 1)
                            {
                                lastBreak = firstCallDef + 1;
                                breakIgnor = false;
                                var result = StackSaves.Pop();
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}