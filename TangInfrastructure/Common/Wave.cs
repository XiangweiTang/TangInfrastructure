using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace TangInfrastructure
{
    class Wave
    {
        public byte[] Bytes { get => _Bytes.ToArray(); }
        private byte[] _Bytes = new byte[0];
        public byte[] DataBytes
        {
            get=> _DataBytes.ToArray();
        }
        private byte[] _DataBytes = new byte[0];
        public List<Chunk> ChunkList { get => _ChunkList.ToList(); }
        private List<Chunk> _ChunkList = new List<Chunk>();
        public Chunk fmt_Chunk { get => _fmt_Chunk.Clone(); }
        private Chunk _fmt_Chunk = new Chunk();
        public Chunk dataChunk { get => _dataChunk.Clone(); }
        private Chunk _dataChunk = new Chunk();

        public short TypeId { get; private set; } = 0;
        public string Type
        {
            get
            {
                switch (TypeId)
                {
                    case 0:
                        return "UNKNOWN";
                    case 1:
                        return "PCM";
                    case 2:
                        return "MSADPCM";
                    case 6:
                        return "ALAW";
                    case 7:
                        return "ULAW";
                    default:
                        throw new TangInfrastructureException("Unsupported audio type.");
                }
            }
        }
        public short NumChannels { get; private set; } = 1;
        public int SampleRate { get; private set; } = 0;
        public int ByteRate { get; private set; } = 0;
        public short BlockAlign { get; private set; } = 0;
        public short BitsPerSample { get; private set; } = 0;
        public double AudioTime { get; private set; } = 0;

        bool IsDeep = true;

        public Wave() { }

        public void ShallowParse(string path)
        {
            var bytes = Common.ReadBytes(path, 100);
            ShallowParse(bytes);
        }

        public void ShallowParse(byte[] bytes)
        {
            Parse(bytes, false);
        }

        public void DeepParse(string path)
        {
            var bytes = File.ReadAllBytes(path);
            DeepParse(bytes);
        }

        public void DeepParse(byte[] bytes)
        {
            Parse(bytes, true);
        }

        private void Parse(byte[] bytes, bool isDeep)
        {
            _Bytes = bytes;
            IsDeep = isDeep;
            ParseRiff();
            PostProcess();
        }

        private void ParseRiff()
        {
            Sanity.RequiresWave(_Bytes.Length >= 44, $"File length is {_Bytes.Length}, the minimum length of wave file is 44.");
            string riffChunkName = Encoding.ASCII.GetString(_Bytes, 0, 4);
            Sanity.RequiresWave(riffChunkName == "RIFF", $"RIFF chunk name error.");
            int riffChunkSize = BitConverter.ToInt32(_Bytes, 4);
            if (IsDeep)
                Sanity.RequiresWave(riffChunkSize + 8 == _Bytes.Length, "RIFF chunk size error.");
            string waveChunkName = Encoding.ASCII.GetString(_Bytes, 8, 4);
            Sanity.RequiresWave(waveChunkName == "WAVE", $"WAVE name error.");
            ParseChunk(12);
        }

        private void ParseChunk(int offset)
        {
            if (offset == _Bytes.Length)
                return;
            if (offset + 8 > _Bytes.Length)
            {
                Sanity.RequiresWave(!IsDeep, $"Chunk breaks at {offset.ToString("x")}.");
                return;
            }
            string chunkName = Encoding.ASCII.GetString(_Bytes, offset, 4);
            int length = BitConverter.ToInt32(_Bytes, offset + 4);
            if ((length & 1) == 1)
                length++;
            Chunk chunk = new Chunk() { Name = chunkName, Offset = offset, Length = length };
            switch (chunkName)
            {
                case "fmt ":
                    _fmt_Chunk = chunk;
                    break;
                case "data":
                    _dataChunk = chunk;
                    break;
                default:
                    _ChunkList.Add(chunk);
                    break;
            }
            ParseChunk(offset + 8 + length);
        }

        private void PostProcess()
        {
            
            Sanity.RequiresWave(!string.IsNullOrWhiteSpace(_fmt_Chunk.Name), "Missing fmt_ chunk.");
            Sanity.RequiresWave(!string.IsNullOrWhiteSpace(_dataChunk.Name), "Missing data chunk.");
            if (IsDeep)
            {
                _DataBytes = new byte[_dataChunk.Length];
                Array.Copy(_Bytes, _dataChunk.Offset + 8, _DataBytes, 0, _dataChunk.Length);
            }
            int fmtOffset = _fmt_Chunk.Offset;

            TypeId = BitConverter.ToInt16(Bytes, fmtOffset + 8);
            NumChannels = BitConverter.ToInt16(Bytes, fmtOffset + 10);
            SampleRate = BitConverter.ToInt32(Bytes, fmtOffset + 12);
            ByteRate = BitConverter.ToInt32(Bytes, fmtOffset + 16);
            BlockAlign = BitConverter.ToInt16(Bytes, fmtOffset + 20);
            BitsPerSample = BitConverter.ToInt16(Bytes, fmtOffset + 22);
            AudioTime = ((double)_dataChunk.Length) / ByteRate;
        }

        public static byte[] StandardPCMHeader(int sampleRate, int sampleNumber)
        {
            byte[] bytes = new byte[44];
            bytes.ArrayPlace(Encoding.ASCII.GetBytes("RIFF"), 0);
            bytes.ArrayPlace(BitConverter.GetBytes(sampleNumber + 36), 4);
            bytes.ArrayPlace(Encoding.ASCII.GetBytes("WAVE"), 8);
            bytes.ArrayPlace(Encoding.ASCII.GetBytes("fmt "), 12);
            bytes.ArrayPlace(BitConverter.GetBytes(16), 16);
            bytes.ArrayPlace(BitConverter.GetBytes((short)1), 20);
            bytes.ArrayPlace(BitConverter.GetBytes((short)1), 22);
            bytes.ArrayPlace(BitConverter.GetBytes(sampleRate), 24);
            bytes.ArrayPlace(BitConverter.GetBytes(sampleRate * 2), 28);
            bytes.ArrayPlace(BitConverter.GetBytes((short)2), 32);
            bytes.ArrayPlace(BitConverter.GetBytes((short)16), 34);
            bytes.ArrayPlace(Encoding.ASCII.GetBytes("data"), 36);
            bytes.ArrayPlace(BitConverter.GetBytes(sampleNumber * 2), 40);
            return bytes;
        }

        public static void ExtendWave(string inputPath, string outputPath, int sampleNumber)
        {
            Wave w = new Wave();
            w.DeepParse(inputPath);
            var dataBytes = w.DataBytes;
            byte[] raw = new byte[sampleNumber * w.BitsPerSample / 8];
            raw.ArrayPlace(dataBytes, 0);
            var bytes= StandardPCMHeader(w.SampleRate, sampleNumber).ArrayConcat(raw);
            File.WriteAllBytes(outputPath, bytes);
        }
    }

    class Chunk
    {
        public string Name = string.Empty;
        public int Offset = 0;
        public int Length = 0;

        public Chunk Clone()
        {
            return new Chunk()
            {
                Name = Name,
                Offset = Offset,
                Length = Length
            };
        }
    }
}
