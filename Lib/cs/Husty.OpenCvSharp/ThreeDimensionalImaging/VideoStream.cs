﻿using System.Text;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using OpenCvSharp;
using Husty.OpenCvSharp.ImageStream;

namespace Husty.OpenCvSharp.ThreeDimensionalImaging;

public class VideoStream : IVideoStream<BgrXyzImage>
{

    // ------ fields ------ //

    private readonly long[] _indexes;
    private readonly BinaryReader _binReader;
    private readonly ObjectPool<BgrXyzImage> _pool;
    private int _positionIndex;
    private long _prevTime;


    // ------ properties ------ //

    public int Fps { get; }

    public int Channels => 6;

    public Size FrameSize { get; }

    public bool HasFrame { private set; get; }

    public int FrameCount => _indexes.Length;

    public int CurrentPosition => _positionIndex;

    public bool IsEnd => _positionIndex >= FrameCount - 1;

    public DateTime InitialTime { get; } = default;

    public DateTime CurrentTime => InitialTime + TimeSpan.FromTicks(_prevTime);


    // ------ constructors ------ //

    public VideoStream(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException();
        var file = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite);
        _binReader = new BinaryReader(file, Encoding.ASCII);
        var fileFormatCode = Encoding.ASCII.GetString(_binReader.ReadBytes(8));
        if (fileFormatCode is not "HUSTY002" and not "HQVDST01")
        {
            file.Close();
            throw new Exception("invalid file format");
        }
        InitialTime = DateTime.FromBinary(_binReader.ReadInt64());
        var indexesPos = _binReader.ReadInt64();
        var indexes = new List<long>();
        if (indexesPos is -1)
        {
            _binReader.BaseStream.Position = 24;
            while (true)
            {
                indexes.Add(_binReader.BaseStream.Position);
                if (_binReader.BaseStream.Position + 8 > _binReader.BaseStream.Length - 1) break;
                _binReader.BaseStream.Position += 8;
                var len0 = _binReader.ReadInt32();
                if (_binReader.BaseStream.Position + len0 > _binReader.BaseStream.Length - 1) break;
                _binReader.BaseStream.Position += len0;
                var len1 = _binReader.ReadInt32();
                if (_binReader.BaseStream.Position + len1 > _binReader.BaseStream.Length - 1) break;
                _binReader.BaseStream.Position += len1;
            }
            indexes.RemoveAt(indexes.Count - 1);

            var binWriter = new BinaryWriter(file, Encoding.ASCII);
            binWriter.Seek(16, SeekOrigin.Begin);
            binWriter.Write(binWriter.BaseStream.Length);
            binWriter.Seek(0, SeekOrigin.End);
            indexes.ForEach(binWriter.Write);
        }
        else
        {
            _binReader.BaseStream.Position = indexesPos;
            while (_binReader.BaseStream.Position < _binReader.BaseStream.Length)
                indexes.Add(_binReader.ReadInt64());
            if (indexes.Count < 5)
                throw new Exception("frame count is too small");
        }
        _indexes = indexes.ToArray();

        _binReader.BaseStream.Seek(_indexes[0], SeekOrigin.Begin);
        _binReader.ReadInt64();
        var userDataSize = _binReader.ReadUInt16();
        if (userDataSize > 0)
            _binReader.Read(new byte[userDataSize], 0, userDataSize);
        _binReader.ReadInt32();
        var bgrDataSize = _binReader.ReadInt32();
        var bgrBytes = _binReader.ReadBytes(bgrDataSize);
        using var frame = Cv2.ImDecode(bgrBytes, ImreadModes.Unchanged);
        FrameSize = new(frame.Width, frame.Height);

        _pool = new(2, () => new(
            new Mat(FrameSize.Height, FrameSize.Width, MatType.CV_8UC3),
            new Mat(FrameSize.Height, FrameSize.Width, MatType.CV_16UC1),
            new Mat(FrameSize.Height, FrameSize.Width, MatType.CV_16UC1),
            new Mat(FrameSize.Height, FrameSize.Width, MatType.CV_16UC1))
        );

        long ticks = 0;
        for (int i = 0; i < 5; i++)
        {
            Read(out var time, out _);
            ticks += time.Ticks;
        }
        ticks /= 5;
        Fps = 1000 / TimeSpan.FromTicks(ticks).Milliseconds;
    }


    // ------ public methods ------ //

    public BgrXyzImage Read()
    {
        return Read(out _, out _);
    }

    public BgrXyzImage Read(out TimeSpan delay, out byte[] userData)
    {
        var frame = _pool.GetObject();
        if (TryRead(frame, out delay, out userData))
            return frame;
        else
            return null;
    }

    public bool TryRead(BgrXyzImage frame)
    {
        return TryRead(frame, out _, out _);
    }

    public bool TryRead(BgrXyzImage frame, out TimeSpan delay, out byte[] userData)
    {
        userData = null;
        if (frame.Width != FrameSize.Width || frame.Height != FrameSize.Height)
        {
            frame.Bgr.Create(FrameSize, MatType.CV_8UC3);
            frame.X.Create(FrameSize, MatType.CV_16UC1);
            frame.Y.Create(FrameSize, MatType.CV_16UC1);
            frame.Z.Create(FrameSize, MatType.CV_16UC1);
        }
        delay = default;
        if (_positionIndex == FrameCount - 1)
            return false;
        _binReader.BaseStream.Seek(_indexes[_positionIndex++], SeekOrigin.Begin);
        var time = _binReader.ReadInt64();
        var ticks = time - _prevTime > 0 ? time - _prevTime : 0;
        delay = TimeSpan.FromTicks(ticks);
        _prevTime = time;
        var userDataSize = _binReader.ReadUInt16();
        if (userDataSize > 0)
        {
            userData = new byte[userDataSize];
            var nRead = _binReader.Read(userData, 0, userDataSize);
            if (nRead < userDataSize) userData = null;
        }
        _binReader.ReadInt32();
        var bgrDataSize = _binReader.ReadInt32();
        var bgrBytes = _binReader.ReadBytes(bgrDataSize);
        var xDataSize = _binReader.ReadInt32();
        var xBytes = _binReader.ReadBytes(xDataSize);
        var yDataSize = _binReader.ReadInt32();
        var yBytes = _binReader.ReadBytes(yDataSize);
        var zDataSize = _binReader.ReadInt32();
        var zBytes = _binReader.ReadBytes(zDataSize);
        frame.CopyFrom(
            Cv2.ImDecode(bgrBytes, ImreadModes.Unchanged), 
            Cv2.ImDecode(xBytes, ImreadModes.Unchanged),
            Cv2.ImDecode(yBytes, ImreadModes.Unchanged),
            Cv2.ImDecode(zBytes, ImreadModes.Unchanged)
        );
        HasFrame = true;
        return true;
    }

    public IObservable<BgrXyzImage> GetStream()
    {
        return Observable.Repeat(0, ThreadPoolScheduler.Instance)
            .Where(_ => _positionIndex < FrameCount)
            .Select(_ =>
            {
                var frame = Read(out var span, out var data);
                if (frame is not null)
                    Task.Delay(span).Wait();
                return frame;
            })
            .TakeUntil(x => x is null)
            .Where(x => x is not null && !x.IsDisposed && !x.Empty())
            .Publish().RefCount();
    }

    public void Seek(int position)
    {
        if (position > -1 && position < FrameCount) _positionIndex = position;
        _binReader.BaseStream.Seek(_indexes[_positionIndex], SeekOrigin.Begin);
        _prevTime = _binReader.ReadInt64();
    }

    public void Dispose()
    {
        HasFrame = false;
        _binReader?.Close();
        _binReader?.Dispose();
    }

}
