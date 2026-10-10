using System.IO;
using CapParse.OCR;
using RapidOcrModelSet = RapidOcrNet.RapidOcrModelSet;

namespace CapParse.Tests;

// Initialization failure path: no real model files are involved, the engine
// is pointed at paths that do not exist and must fail from the constructor
// without leaving a usable engine behind.
public class RapidOcrEngineTests
{
    [Fact]
    public void Ctor_MissingModelFiles_ThrowsFromConstructor()
    {
        var missing = RapidOcrModelSet.PPOCRv6Small with
        {
            DetModelPath = Path.Combine(Path.GetTempPath(), "capparse-m7b-missing-det.onnx"),
            ClsModelPath = Path.Combine(Path.GetTempPath(), "capparse-m7b-missing-cls.onnx"),
            RecModelPath = Path.Combine(Path.GetTempPath(), "capparse-m7b-missing-rec.onnx"),
            KeysPath = Path.Combine(Path.GetTempPath(), "capparse-m7b-missing-dict.txt")
        };

        Assert.ThrowsAny<Exception>(() => new RapidOcrEngine(missing));
    }
}
