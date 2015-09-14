using System;
using System.Collections;
using System.Linq;
using BizTalkComponents.Utils;

namespace BizTalkComponents.PipelineComponents.DecryptMessage
{
    public partial class DecryptMessage
    {
        public string Name { get { return "DecryptMessage"; } }
        public string Version { get { return "1.0"; } }
        public string Description
        {
            get
            {
                return
                    "Decrypts the message using AES encryption.";
            }
        }

        public void GetClassID(out Guid classID)
        {
            classID = Guid.Parse("040DDBA5-E1C6-4C1B-AAFA-30CBBFB80194");
        }

        public void InitNew()
        {
            
        }

        public bool Validate(out string errorMessage)
        {
            var errors = ValidationHelper.Validate(this, true).ToArray();

            if (errors.Any())
            {
                errorMessage = string.Join(",", errors);

                return false;
            }

            errorMessage = string.Empty;

            return true;
        }

        public IEnumerator Validate(object projectSystem)
        {
            return ValidationHelper.Validate(this, false).ToArray().GetEnumerator();
        }

        public IntPtr Icon { get { return IntPtr.Zero; } }
    }
}