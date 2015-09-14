using System;
using System.IO;
using System.Security.Cryptography;
using BizTalkComponents.Utils.LookupUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Winterdom.BizTalk.PipelineTesting;

namespace BizTalkComponents.PipelineComponents.DecryptMessage.Tests.UnitTests
{
    [TestClass]
    public class DecryptMessageTests
    {
        [TestMethod]
        public void TestSuccessful()
        {
            var keyStr = "AAECAwQFBgcICQoLDA0ODw==";
            var key = Convert.FromBase64String(keyStr);

            var mock = new Mock<ISSOLookupRepository>();
            mock.Setup(r => r.Read("SSOApplication", "SSOKey")).Returns(keyStr);

            var pipeline = PipelineFactory.CreateEmptySendPipeline();

            var em = new DecryptMessage(mock.Object)
            {
                SSOConfigApplication = "SSOApplication",
                SSOConfigKey = "SSOKey"
            };

            pipeline.AddComponent(em, PipelineStage.Encode);
            string input = "<test></test>";
            var d = MessageHelper.CreateFromString(input);
            var data = EncryptMessage(d.BodyPart.Data, key);
            var msg = MessageHelper.CreateFromStream(data);
            var output = pipeline.Execute(msg);
            string str;
            using (var sr = new StreamReader(output.BodyPart.Data))
            {
                str = sr.ReadToEnd();
            }

            Assert.AreEqual(input,str.Replace("\0",string.Empty));
        }

        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void TestWrongKey()
        {
            var keyStr = "wrtrl3lY8/C7xUpLQX1rP3+ve8wwGAEQY4HX+5YhylM=";
            
            var key = Convert.FromBase64String(keyStr);

            var keyStrWrong = "sj38COroD3UbaLS/JobOwHjSfvHKUBgm9XtiRMDgazk=";
            var keyWrong = Convert.FromBase64String(keyStrWrong);

            var mock = new Mock<ISSOLookupRepository>();
            mock.Setup(r => r.Read("SSOApplication", "SSOKey")).Returns(keyStr);

            var pipeline = PipelineFactory.CreateEmptySendPipeline();

            var em = new DecryptMessage(mock.Object)
            {
                SSOConfigApplication = "SSOApplication",
                SSOConfigKey = "SSOKey"
            };

            pipeline.AddComponent(em, PipelineStage.Encode);
            string input = "<test></test>";
            var d = MessageHelper.CreateFromString(input);
            var data = EncryptMessage(d.BodyPart.Data, keyWrong);
            var msg = MessageHelper.CreateFromStream(data);
            var output = pipeline.Execute(msg);
            string str;
            using (var sr = new StreamReader(output.BodyPart.Data))
            {
                str = sr.ReadToEnd();
            }

            Assert.AreNotEqual(input, str.Replace("\0", string.Empty));
        }

        private Stream EncryptMessage(Stream data, byte[] key)
        {
            var ms = new MemoryStream();

            using (var provider = new AesCryptoServiceProvider())
            {
                provider.Key = key;
                provider.Mode = CipherMode.CBC;
                provider.Padding = PaddingMode.PKCS7;

                using (var encryptor = provider.CreateEncryptor(provider.Key, provider.IV))
                {
                    ms.Write(provider.IV, 0, 16);

                    var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
                    data.CopyTo(cs);
                    cs.FlushFinalBlock();
                }
            }

            ms.Seek(0, SeekOrigin.Begin);
            ms.Position = 0;
            
            return ms;
        }
    }
}
