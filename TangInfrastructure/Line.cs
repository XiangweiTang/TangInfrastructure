using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    abstract class Line
    {
        public string SessionId { get; protected set; } = string.Empty;
        public string FileName { get; protected set; } = string.Empty;
        public string Text { get; protected set; } = string.Empty;
        public Line(string line)
        {
            Set(line);
        }
        public void UpdateText(string newText)
        {
            Text = newText;
        }
        public string Output => string.Join("\t", Get());
        abstract protected void Set(string line);
        abstract protected IEnumerable<object> Get();
    }
    class TcLine : Line
    {
        public double StartTime { get; protected set; } = 0;
        public double EndTime { get; protected set; } = 0;
        public double Duration { get; protected set; } = 0;
        public string SrcAudioPath { get; protected set; } = string.Empty;
        public TcLine(string line) : base(line)
        {

        }

        protected override IEnumerable<object> Get()
        {
            yield return FileName;
            yield return SessionId;
            yield return StartTime;
            yield return EndTime;
            yield return Text;
            yield return SrcAudioPath;
        }

        protected override void Set(string line)
        {
            var split = line.Split('\t');
            Sanity.Requires(split.Length == 7, "Invalid TcLine.\t" + line);
            FileName = split[0];
            SessionId = split[1];
            StartTime = double.Parse(split[2]);
            EndTime = double.Parse(split[3]);
            Text = split[4];
            SrcAudioPath = split[5];
            Duration = EndTime - StartTime;
        }
    }

    class PhonLine : TcLine
    {
        public PhonLine(string line) : base(line) { }
        public int SentenceId { get; private set; } = 0;
        public string Word { get; private set; } = string.Empty;
        protected override IEnumerable<object> Get()
        {
            yield return FileName;
            yield return SessionId;
            yield return SentenceId;
            yield return Word;
            yield return Text;
            yield return StartTime;
            yield return Duration;
            yield return SrcAudioPath;
        }

        protected override void Set(string line)
        {
            var split = line.Split('\t');
            Sanity.Requires(split.Length == 8);
            FileName = split[0];
            SessionId = split[1];
            SentenceId = int.Parse(split[2]);
            Word = split[3];
            Text = split[4];
            StartTime = double.Parse(split[5]);
            Duration = double.Parse(split[6]);
            SrcAudioPath = split[7];
        }
    }
}
