using System;
using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BizTalkComponents.Utils;
using BizTalkComponents.Utils.LookupUtils;
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
        private readonly ISSOLookupRepository _ssoConfigRepository;

        private const string SSOConfigApplicationPropertyName = "SSOConfigApplicationProperty";
        private const string SSOConfigKeyPropertyName = "SSOConfigKey";

        [RequiredRuntime]
        [DisplayName("SSO Config Application")]
        [Description("The name of the SSO Application to read the encryption key from.")]
        public string SSOConfigApplication { get; set; }

        [RequiredRuntime]
        [DisplayName("SSO Config Key")]
        [Description("The key of the SSO configuraiton property to read the encryption key from.")]
        public string SSOConfigKey { get; set; }

        public DecryptMessage(ISSOLookupRepository ssoConfigRepository)
        {
            _ssoConfigRepository = ssoConfigRepository;
        }

        public DecryptMessage()
        {
            _ssoConfigRepository = new SSOLookupRepository();
        }

        public IBaseMessage Execute(IPipelineContext pContext, IBaseMessage pInMsg)
        {
            string errorMessage;

            if (!Validate(out errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            var sr = new SSOLookupManager(_ssoConfigRepository);
            var encryptionKeyStr = sr.GetConfigValue(SSOConfigApplication, SSOConfigKey);
            if (string.IsNullOrEmpty(encryptionKeyStr))
            {
                throw new ArgumentException("No encryption key could be found at the specified sso config.");
            }

            var encryptionKey = Convert.FromBase64String(encryptionKeyStr);

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
            SSOConfigApplication = PropertyBagHelper.ReadPropertyBag<string>(propertyBag, SSOConfigApplicationPropertyName);
            SSOConfigKey = PropertyBagHelper.ReadPropertyBag<string>(propertyBag, SSOConfigKeyPropertyName);
        }

        public void Save(IPropertyBag propertyBag, bool clearDirty, bool saveAllProperties)
        {
            PropertyBagHelper.WritePropertyBag(propertyBag, SSOConfigApplicationPropertyName, SSOConfigApplication);
            PropertyBagHelper.WritePropertyBag(propertyBag, SSOConfigKeyPropertyName, SSOConfigKey);
        }
    }
}
