using System;
using System.Security.Cryptography.X509Certificates;

namespace FM.LiveSwitch.Hammer
{
    [Serializable]
    public class CertificateExpiringException : Exception
    {
        public X509Certificate2 Certificate { get; private set; }

        public CertificateExpiringException(string message, X509Certificate2 certificate)
            : base(message)
        {
            Certificate = certificate;
        }
    }
}