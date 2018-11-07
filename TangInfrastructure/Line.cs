using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangInfrastructure
{
    abstract class Line
    {
        public string SpeakerId { get; protected set; } = string.Empty;
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
        public double StartTime { get; private set; } = 0;
        public double EndTime { get; private set; } = 0;
        public double Duration => EndTime - StartTime;
        public string SrcAudioPath { get; private set; } = string.Empty;
        public TcLine(string line) : base(line)
        {

        }

        protected override IEnumerable<object> Get()
        {
            yield return FileName;
            yield return SpeakerId;
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
            SpeakerId = split[1];
            SessionId = split[2];
            StartTime = double.Parse(split[3]);
            EndTime = double.Parse(split[4]);
            Text = split[5];
            SrcAudioPath = split[6];
        }
    }
}
