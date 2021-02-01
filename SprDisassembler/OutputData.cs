using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace SprDisassembler {
    class OutputData {
        List<string> Text = new List<string>();
        List<int> PCs = new List<int>();
        private readonly TextWriter _writer;

        public OutputData(string filename = null) {
            if (filename is not null) {
                _writer = new StreamWriter(filename);
            } else {
                _writer = Console.Out;
            }
        }

        public void WriteLine(string text, int pc) {
            PCs.Add(pc);
            Text.Add(text);
        }

        public void WriteLineAtPc(string label, int pc) {
            int index = PCs.FindIndex(x => x == pc);
            if (index == -1) {
                WriteLine(label, pc);
                return;
            }
            Text.Insert(index, label);
            PCs.Insert(index, pc);      // lines with just labels do this
        }

        public string this[int index] {
            get => Text[index];
        }

        public void Flush() {
            SortedDictionary<int, string> lines = new SortedDictionary<int, string>();
            for (int i = 0; i < Text.Count; i++) {
                if (lines.ContainsKey(PCs[i])) {
                    if (!lines[PCs[i]].ToLower().EndsWith(Text[i].ToLower())) {
                        lines[PCs[i]] = lines[PCs[i]] + '\n' + Text[i];
                    }
                } else {
                    lines.Add(PCs[i], Text[i]);
                }
            }
            lines.ToList().ForEach(x => _writer.WriteLine(x.Value));
            _writer.Flush();
        }

        public void Close() {
            Flush();
            _writer.Close();
        }
    }
}
