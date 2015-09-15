using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BizTalkComponents.Utils;
using Microsoft.BizTalk.Component.Interop;
using Microsoft.BizTalk.Message.Interop;
using IComponent = Microsoft.BizTalk.Component.Interop.IComponent;

namespace BizTalkComponents.PipelineComponents.DecryptMessage
{

    [ComponentCategory(CategoryTypes.CATID_PipelineComponent)]
    [Guid("5C8BEDAD-F17B-4A3F-BF91-D76A846CF315")]
    [ComponentCategory(CategoryTypes.CATID_Decoder)]
    public partial class DecryptMessage : IComponent, IBaseComponent,
                                        IPersistPropertyBag, IComponentUI
    {
        private const string EncryptionKeyPropertyName = "EncryptionKey";

        [RequiredRuntime]
        [DisplayName("Encryption key")]
        [Description("The key to use when encrypting. Should be in base64 format.")]
        public string EncryptionKey { get; set; }

        public IBaseMessage Execute(IPipelineContext pContext, IBaseMessage pInMsg)
        {
            string errorMessage;

            if (!Validate(out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            var encryptionKey = Convert.FromBase64String(EncryptionKey);

            using (var provider = new AesCryptoServiceProvider())
            {
                provider.Key = encryptionKey;
                provider.Mode = CipherMode.CBC;
                provider.Padding = PaddingMode.PKCS7;

                var ms = new MemoryStream();
                pInMsg.BodyPart.Data.CopyTo(ms);

                ms.Seek(0, SeekOrigin.Begin);
                ms.Position = 0;
                byte[] iv = new byte[16];
                ms.Read(iv, 0, 16);

                provider.IV = iv;
                
                var decryptor = provider.CreateDecryptor(provider.Key, provider.IV);

                pInMsg.BodyPart.Data = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            }

            return pInMsg;
        }



         public void Load(IPropertyBag propertyBag, int errorLog)
        {
            EncryptionKey = PropertyBagHelper.ReadPropertyBag<string>(propertyBag, EncryptionKeyPropertyName);
        }

        public void Save(IPropertyBag propertyBag, bool clearDirty, bool saveAllProperties)
        {
            PropertyBagHelper.WritePropertyBag(propertyBag, EncryptionKeyPropertyName, EncryptionKey);
        }
    }
}
