using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace BackupManagerLibrary
{
    public class EmailSender
    {
        public List<string> ToAddresses { get; set; } = new List<string>();
        public string FromAddress { get; set; }
        public string FromName { get; set; }

        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUserName;
        private readonly string _smtpPassword;

        public EmailSender(string toAddress, string fromAddress, string fromName, string smtpServer, int smtpPort, string smtpUserName, string smtpPassword) : this(smtpServer, smtpPort, smtpUserName, smtpPassword) {
            this.ToAddresses.Add(toAddress);
            this.FromAddress = fromAddress;
            this.FromName = fromName;
        }

        public EmailSender(string smtpServer, int smtpPort, string smtpUserName, string smtpPassword) {
            this._smtpServer = smtpServer;
            this._smtpPort = smtpPort;
            this._smtpUserName = smtpUserName;
            this._smtpPassword = smtpPassword;
        }

        public void SendEmail(string toAddress, string fromAddress, string fromName, string subject, string bodyHtml) {
            this.ToAddresses.Clear();
            this.ToAddresses.Add(toAddress);
            this.FromAddress = fromAddress;
            this.FromName = fromName;
            SendEmail(subject, bodyHtml);
        }

        public void SendEmail(string subject, string bodyHtml) {
            using (MailMessage mailMessage = new MailMessage())
            using (SmtpClient smtpClient = new SmtpClient(_smtpServer, _smtpPort)) {
                foreach (string toAddress in ToAddresses) {
                    mailMessage.To.Add(toAddress);
                }
                mailMessage.From = new MailAddress(FromAddress, FromName);
                mailMessage.ReplyToList.Add(FromAddress);
                mailMessage.IsBodyHtml = true;
                mailMessage.Subject = subject;
                mailMessage.Body = bodyHtml;
                smtpClient.Credentials = new NetworkCredential(_smtpUserName, _smtpPassword);
                smtpClient.EnableSsl = true;
                smtpClient.Timeout = 60 * 1000; // 60 seconds
                smtpClient.Send(mailMessage);
            }
        }
    }
}
