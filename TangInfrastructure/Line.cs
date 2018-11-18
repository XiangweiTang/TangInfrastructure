using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;

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

    abstract class NewLine
    {

        public string CorpusName { get; protected set; } = string.Empty;
        public string SpeakerId { get; protected set; } = "U";
        public string SessionId { get; protected set; } = string.Empty;
        public string InternalId { get; protected set; } = string.Empty;
        public string Transcription { get; protected set; } = string.Empty;

        public NewLine() { }

        public NewLine(string line)
        {
            Set(line);
        }

        public NewLine(string corpusName, string sessionId, string speakerId, string internalId, string transcription)
        {
            CorpusName = corpusName;
            SessionId = sessionId;
            SpeakerId = speakerId;
            InternalId = internalId;
            Transcription = transcription;
        }

        public NewLine(NewLine line)
        {
            CorpusName = line.CorpusName;
            SessionId = line.SessionId;
            SpeakerId = line.SpeakerId;
            InternalId = line.InternalId;
            Transcription = line.Transcription;
        }

        public NewLine(string internalId, bool unified = true, params NewLine[] lines)
        {
            Sanity.Requires(lines.Length > 0, "Merge lines requires at least one line(s).");
            CorpusName = lines[0].CorpusName;
            SpeakerId = unified ? lines[0].SpeakerId : "U";
            SessionId = lines[0].SessionId;
            InternalId = internalId;
            Transcription = string.Join(" ", lines.Select(x => x.Transcription));
        }

        public string Output => string.Join("\t", Get());

        public string AudioPath(string audioRootPath)
        {
            return Path.Combine(audioRootPath, CorpusName, SpeakerId, SessionId, InternalId + ".wav");
        }

        public void UpdateTranscript(string newTrans)
        {
            Transcription = newTrans;
        }

        abstract protected void Set(string line);
        abstract protected IEnumerable<object> Get();
    }

    class NewTcLine : NewLine
    {
        public double StartTime { get; protected set; } = 0;
        public double EndTime { get; protected set; } = 0;
        public double Duration => EndTime - StartTime;
        public string SrcAudioPath { get; protected set; } = string.Empty;

        public NewTcLine() : base() { }

        public NewTcLine(string line) : base(line)
        {

        }

        public NewTcLine(string corpusName, string speakerId, string sessionId, string internalId, double startTime, double endTime, string transcription, string srcAudioPath)
            : base(corpusName, sessionId, speakerId, internalId, transcription)
        {
            StartTime = startTime;
            EndTime = endTime;
            SrcAudioPath = srcAudioPath;
        }

        public NewTcLine(string corpusName, string speakerId, string sessionId, string internalId, string startTime, string endTime, string transcription, string srcAudioPath)
            : base(corpusName, sessionId, speakerId, internalId, transcription)
        {
            StartTime = double.Parse(startTime);
            EndTime = double.Parse(endTime);
            SrcAudioPath = srcAudioPath;
        }

        public NewTcLine(string internalId, bool unified = true,params NewTcLine[] lines) : base(internalId, unified, lines)
        {
            StartTime = lines[0].StartTime;
            EndTime = lines.Last().EndTime;
            SrcAudioPath = lines[0].SrcAudioPath;
        }

        public void SetStartTime(double startTime)
        {
            StartTime = startTime;
        }

        public void SetEndTime(double endTime)
        {
            EndTime = endTime;
        }

        protected override IEnumerable<object> Get()
        {
            yield return CorpusName;
            yield return SpeakerId;
            yield return SessionId;
            yield return InternalId;
            yield return StartTime;
            yield return EndTime;
            yield return Transcription;
            yield return SrcAudioPath;
        }

        protected override void Set(string line)
        {
            var split = line.Split('\t');
            Sanity.RequiresLine(split.Length == 8, "TcLine");
            CorpusName = split[0];
            SpeakerId = split[1];
            SessionId = split[2];
            InternalId = split[3];
            StartTime = double.Parse(split[4]);
            EndTime = double.Parse(split[5]);
            Transcription = split[6];
            SrcAudioPath = split[7];
        }
    }

    class OpusLine : NewTcLine
    {
        public string Locale { get; private set; } = string.Empty;
        public OpusLine(string line) : base(line) { }
        public OpusLine(string locale, string corpusName, string speakerId, string sessionId, string internalId, double startTime, double endTime,string transcription, string srcAudioPath = "")
        : base(corpusName, speakerId, sessionId, internalId, startTime, endTime, transcription, srcAudioPath)
        {
            Locale = locale;
        }
        public OpusLine(string locale, string corpusName, string speakerId, string sessionId, string internalId, string startTime, string endTime, string transcription, string srcAudioPath="") 
            :base(corpusName,speakerId, sessionId, internalId, startTime, endTime, transcription, srcAudioPath)
        {
            Locale = locale;
        }
        protected override IEnumerable<object> Get()
        {
            yield return Locale;
            yield return CorpusName;
            yield return SpeakerId;
            yield return SessionId;
            yield return InternalId;
            yield return StartTime;
            yield return EndTime;
            yield return Transcription;
            yield return SrcAudioPath;
        }

        protected override void Set(string line)
        {
            var split = line.Split('\t');
            Sanity.RequiresLine(split.Length == 9, "Opus line");
            Locale = split[0];
            CorpusName = split[1];
            SpeakerId = split[2];
            SessionId = split[3];
            InternalId = split[4];
            StartTime = double.Parse(split[5]);
            EndTime = double.Parse(split[6]);
            Transcription = split[7];
            SrcAudioPath = split[8];
        }
    }
}
