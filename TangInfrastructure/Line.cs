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

        public string CorpusName { get; protected set; } = string.Empty;
        public string SpeakerId { get; protected set; } = "U";
        public string SessionId { get; protected set; } = string.Empty;
        public string InternalId { get; protected set; } = string.Empty;
        public string Transcription { get; protected set; } = string.Empty;

        public Line() { }

        public Line(string line)
        {
            Set(line);
        }

        public Line(string corpusName, string sessionId, string speakerId, string internalId, string transcription)
        {
            CorpusName = corpusName;
            SessionId = sessionId;
            SpeakerId = speakerId;
            InternalId = internalId;
            Transcription = transcription;
        }

        public Line(Line line)
        {
            CorpusName = line.CorpusName;
            SessionId = line.SessionId;
            SpeakerId = line.SpeakerId;
            InternalId = line.InternalId;
            Transcription = line.Transcription;
        }

        public Line(string internalId, bool unified = true, params Line[] lines)
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

    class TcLine : Line
    {
        public double StartTime { get; protected set; } = 0;
        public double EndTime { get; protected set; } = 0;
        public double Duration => EndTime - StartTime;
        public string SrcAudioPath { get; protected set; } = string.Empty;

        public TcLine() : base() { }

        public TcLine(string line) : base(line)
        {

        }

        public TcLine(string corpusName, string speakerId, string sessionId, string internalId, double startTime, double endTime, string transcription, string srcAudioPath)
            : base(corpusName, sessionId, speakerId, internalId, transcription)
        {
            StartTime = startTime;
            EndTime = endTime;
            SrcAudioPath = srcAudioPath;
        }

        public TcLine(string corpusName, string speakerId, string sessionId, string internalId, string startTime, string endTime, string transcription, string srcAudioPath)
            : base(corpusName, sessionId, speakerId, internalId, transcription)
        {
            StartTime = double.Parse(startTime);
            EndTime = double.Parse(endTime);
            SrcAudioPath = srcAudioPath;
        }

        public TcLine(string internalId, bool unified = true,params TcLine[] lines) : base(internalId, unified, lines)
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

    class OpusLine : TcLine
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
