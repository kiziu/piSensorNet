using System;
using System.Linq;
using piMasterSharp.Extensions;
using piSensorNet.Radio.NrfNet.Enums;

namespace piSensorNet.Radio.NrfNet.Registers
{
    public class FeatureRegister : IRegister
    {
        public RegisterEnum Type { get; } = RegisterEnum.Feature;
        public byte Value => this;

        public bool DynamicPayloadLengthEnabled { get; set; }
        public bool AcknowledgeWithPayloadEnabled { get; set; }
        public bool DynamicAcknowledgeEnabled { get; set; }

        private FeatureRegister() {}

        public FeatureRegister(bool dynamicPayloadLengthEnabled, bool acknowledgeWithPayloadEnabled, bool dynamicAcknowledgeEnabled)
        {
            DynamicPayloadLengthEnabled = dynamicPayloadLengthEnabled;
            AcknowledgeWithPayloadEnabled = acknowledgeWithPayloadEnabled;
            DynamicAcknowledgeEnabled = dynamicAcknowledgeEnabled;
        }

        public override string ToString()
        {
            return $"[DynamicPayloadLengthEnabled: {DynamicPayloadLengthEnabled}, PayloadWithAcknowledgeEnabled: {AcknowledgeWithPayloadEnabled}, DynamicAcknowledgeEnabled: {DynamicAcknowledgeEnabled}]";
        }

        public static implicit operator byte(FeatureRegister input)
        {
            byte output = 0;

            ByteExtensions.SetBit(ref output, 0, input.DynamicAcknowledgeEnabled);
            ByteExtensions.SetBit(ref output, 1, input.AcknowledgeWithPayloadEnabled);
            ByteExtensions.SetBit(ref output, 2, input.DynamicPayloadLengthEnabled);

            return output;
        }

        public static implicit operator FeatureRegister(byte input)
        {
            var output = new FeatureRegister
                         {
                             DynamicAcknowledgeEnabled = input.GetBit(0),
                             AcknowledgeWithPayloadEnabled = input.GetBit(1),
                             DynamicPayloadLengthEnabled = input.GetBit(2),
                         };

            return output;
        }
    }
}