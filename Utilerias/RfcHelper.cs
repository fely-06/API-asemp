using Org.BouncyCastle.X509;
using System.Text.RegularExpressions;

namespace API_asemp.Utilerias
{
    public static class RfcHelper
    {
        public static string? ExtraerRFC(byte[] cerBytes)
        {
            try
            {
                var parser = new X509CertificateParser();
                var bc = parser.ReadCertificate(cerBytes);

                string? rfc = null;
                var oids = bc.SubjectDN.GetOidList();
                for (int i = 0; i < oids.Count; i++)
                {
                    var oid = (Org.BouncyCastle.Asn1.DerObjectIdentifier)oids[i];
                    var vals = bc.SubjectDN.GetValueList(oid);
                    if (vals.Count == 0) continue;

                    var val = vals[0]?.ToString() ?? string.Empty;

                    // OID 2.5.4.45 → RFC=...
                    if (oid.Id == "2.5.4.45")
                    {
                        var m = Regex.Match(val, @"RFC\s*=\s*([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3})", RegexOptions.IgnoreCase);
                        if (m.Success) return m.Groups[1].Value.ToUpperInvariant();
                    }

                    // OID 2.5.4.5 → SERIALNUMBER
                    if (oid.Id == "2.5.4.5")
                    {
                        var m = Regex.Match(val, @"RFC\s*=\s*([A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3})", RegexOptions.IgnoreCase);
                        if (m.Success) return m.Groups[1].Value.ToUpperInvariant();

                        m = Regex.Match(val, @"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", RegexOptions.IgnoreCase);
                        if (m.Success) return m.Value.ToUpperInvariant();
                    }
                }

                // Fallback: escanear todo el DN
                var dn = bc.SubjectDN.ToString();
                var cand = Regex.Matches(dn, @"[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}")
                    .Select(m => m.Value.ToUpperInvariant())
                    .FirstOrDefault();

                return cand;
            }
            catch
            {
                return null;
            }
        }
    }
}
