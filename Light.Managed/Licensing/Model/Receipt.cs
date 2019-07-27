using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Light.Managed.Licensing.Model
{
    internal class Receipt
    {
        [XmlElement] public AppReceipt AppReceipt;
        [XmlElement] public ProductReceipt ProductReceipt;
        [XmlElement] public Signature Signature;

        [XmlAttribute] public string Version;
        [XmlAttribute] public string CertificateId;
        [XmlAttribute] public DateTime ReceiptDate;
        [XmlAttribute] public string ReceiptDeviceId;
    }

    internal class AppReceipt
    {
        [XmlAttribute] public string Id;
        [XmlAttribute] public string AppId;
        [XmlAttribute] public string LicenseType;
        [XmlAttribute] public DateTime PurchaseDate;
    }

    internal class ProductReceipt
    {
        [XmlAttribute] public string Id;
        [XmlAttribute] public string AppId;
        [XmlAttribute] public string ProductId;
        [XmlAttribute] public string ProductType;
        [XmlAttribute] public DateTime PurchaseDate;
    }

    internal class Signature
    {
        [XmlElement] public SignedInfo SignedInfo;
        [XmlElement] public string SignatureValue;
        [XmlElement] public KeyInfo KeyInfo;
    }

    internal class KeyInfo
    {
        [XmlElement] public Value KeyValue;

        public class Value
        {
            [XmlElement("DSAKeyValue")]public DValue DsaKeyValue;

            public class DValue
            {
                public string P;
                public string Q;
                public string G;
                public string Y;
            }
        }
    }

    internal class SignedInfo
    {
        [XmlElement] public Method CanonicalizationMethod;
        [XmlElement] public Method SignatureMethod;
        [XmlElement] public Method DigestMethod;
        [XmlElement] public Ref Reference;
        [XmlElement] public string DigestValue;

        internal class Method
        {
            [XmlAttribute]
            public string Algorithm;
        }

        internal class Ref
        {
            [XmlAttribute("URI")]
            public string Uri;
            [XmlElement] public IEnumerable<Transform> Transforms;

            public class Transform
            {
                public string Algorithm;
            }
        }
    }

}
