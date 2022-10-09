using System.Net.Mail;
using System.Text;

namespace MailMod
{
	internal struct MailSetting
	{
		internal string MailTo;
		internal string MailFrom;
		internal List<string> MailCC = new();
		internal List<string> MailBCC = new();
		internal string Subject = "";
		internal string Body = "";
		internal bool IsHTML = false;
		internal Attachment? Attackment = null;
		internal Encoding Encoding = Encoding.UTF8;

		internal MailSetting(string mailTo, string mailFrom)
		{
			MailTo = mailTo;
			MailFrom = mailFrom;
		}
	}

	internal class MailClient
	{

		internal static string? SMTPServer;

		internal static void Init(string smtpServer)
		{
			SMTPServer = smtpServer;
		}

		internal bool Send(MailSetting mailSetting)
		{
			if (SMTPServer == null)
			{
				Error("SMTPサーバを指定してください。");
				return false;
			}

			MailMessage message = new(mailSetting.MailFrom, mailSetting.MailTo);
			foreach (var CC in mailSetting.MailCC)
			{
				message.CC.Add(CC);
			}
			foreach (var BCC in mailSetting.MailBCC)
			{
				message.Bcc.Add(BCC);
			}

			message.Subject = mailSetting.Subject;
			message.Body = mailSetting.Body;
			message.IsBodyHtml = mailSetting.IsHTML;

			try
			{
				using var client = new SmtpClient(SMTPServer);
				client.Send(message);
				return true;
			}
			catch (System.Exception)
			{
				return false;
			}
		}

		private static void Error(string message)
		{
			Console.WriteLine(" ***** SMTP実行時エラー ***** ");
			Console.WriteLine(message);
			Console.WriteLine("");

			Environment.Exit(-1);
		}
	}
}




