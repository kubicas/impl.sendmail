namespace sendmail
{
   class sendmail
   {
      static void Main(string[] args)
      {
         Microsoft.Win32.RegistryKey key = 
            Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
               "Software",
               true);

         key.CreateSubKey("SendMail");
         key = key.OpenSubKey("SendMail", true);


         key.CreateSubKey("1");
         key = key.OpenSubKey("1", true);

         if( args.Length < 1 )
         {
            System.Console.WriteLine("Expected at least 1 command line argument");
            return;
         }

         if (args[0].Equals("set", System.StringComparison.OrdinalIgnoreCase))
         {
            if (args.Length < 3)
            {
               System.Console.WriteLine("Expected at least 3 command line arguments");
               return;
            }

            if (args[1].Equals("smtp", System.StringComparison.OrdinalIgnoreCase))
            {
               key.SetValue("smtp", args[2]);
               System.Console.WriteLine("Done");
            }
            else if (args[1].Equals(
               "port",
               System.StringComparison.OrdinalIgnoreCase))
            {
               key.SetValue("port", args[2]);
               System.Console.WriteLine("Done");
            }
            else if (args[1].Equals("to", System.StringComparison.OrdinalIgnoreCase))
            {
               key.SetValue("to", args[2]);
               System.Console.WriteLine("Done");
            }
            else if (args[1].Equals(
               "from",
               System.StringComparison.OrdinalIgnoreCase))
            {
               key.SetValue("from", args[2]);
               System.Console.WriteLine("Done");
            }
            else
            {
               System.Console.WriteLine("Don't know what to set");
            }
            return;
         }

         else if (args[0].Equals("get", System.StringComparison.OrdinalIgnoreCase))
         {
               if (args.Length < 2)
               {
                  System.Console.WriteLine(
                     "Expected at least 2 command line arguments");
                  return;
               }

               if (args[1].Equals("smtp", System.StringComparison.OrdinalIgnoreCase))
               {
                  System.Console.WriteLine(key.GetValue("smtp"));
               }
               else if (args[1].Equals(
                  "port",
                  System.StringComparison.OrdinalIgnoreCase))
               {
                  System.Console.WriteLine(key.GetValue("port"));
               }
               else if (args[1].Equals(
                  "to",
                  System.StringComparison.OrdinalIgnoreCase))
               {
                  System.Console.WriteLine(key.GetValue("to"));
               }
               else if (args[1].Equals(
                  "from",
                  System.StringComparison.OrdinalIgnoreCase))
               {
                  System.Console.WriteLine(key.GetValue("from"));
               }
               else
               {
                  System.Console.WriteLine("Don't know what to get");
               }
               return;
         }

         else if (args[0].Equals("clear", System.StringComparison.OrdinalIgnoreCase))
         {
            if (args.Length < 2)
            {
               System.Console.WriteLine(
                  "Expected at least 2 command line arguments");
               return;
            }

            if (args[1].Equals("smtp", System.StringComparison.OrdinalIgnoreCase))
            {
               key.DeleteValue("smtp");
               System.Console.WriteLine("Done");
            }
            else if (args[1].Equals(
               "port",
               System.StringComparison.OrdinalIgnoreCase))
            {
               key.DeleteValue("port");
               System.Console.WriteLine("Done");
            }
            else if (args[1].Equals(
               "to",
               System.StringComparison.OrdinalIgnoreCase))
            {
               key.DeleteValue("to");
               System.Console.WriteLine("Done");
            }
            else if (args[1].Equals(
               "from",
               System.StringComparison.OrdinalIgnoreCase))
            {
               key.DeleteValue("from");
               System.Console.WriteLine("Done");
            }
            else
            {
               System.Console.WriteLine("Don't know what to clear");
            }
            return;
         }

         string smtp = (string)key.GetValue("smtp");
         if (string.IsNullOrEmpty(smtp))
         {
            System.Console.WriteLine("smtp not set");
            return;
         }

         string sport = (string)key.GetValue("port");
         if (string.IsNullOrEmpty(sport))
         {
            System.Console.WriteLine("port not set");
            return;
         }

         int port;
         try
         {
            port = System.Convert.ToInt32(sport);
         }
         catch (System.FormatException)
         {
            System.Console.WriteLine("Port is not a sequence of digits.");
            return;
         }
         catch (System.OverflowException)
         {
            System.Console.WriteLine("Port cannot fit in an Int32.");
            return;
         }

         string to = (string)key.GetValue("to");
         if (string.IsNullOrEmpty(to))
         {
            System.Console.WriteLine("to not set");
            return;
         }

         string from1 = (string)key.GetValue("from");

         System.IO.StreamReader patch_file;
         try
         {
            patch_file = new System.IO.StreamReader( args[0] );
         }
         catch
         {
            System.Console.WriteLine("Cannot open file {0}", args[0]);
            return;
         }

         string from2 = null;
         string subject = null;

         // check the first 25 lines
         int line_count = 25;
         do
         {
            string line = patch_file.ReadLine();
            if (string.IsNullOrEmpty(line))
            {
               break;
            }
            if (line.StartsWith("Subject: "))
            {
               subject = line.Substring(9);
            }
            if (line.StartsWith("From: "))
            {
               from2 = line.Substring(6);
            }
            --line_count;
         }
         while (line_count > 0);

         if (string.IsNullOrEmpty(subject))
         {
            System.Console.WriteLine("'Subject: ' not found in file {0}", args[0]);
            return;
         }

         System.Net.Mail.SmtpClient m;
         try
         {
            m = new System.Net.Mail.SmtpClient(smtp, port);
         }
         catch
         {
            System.Console.WriteLine(
               "Cannot connect to {0} port {1}", 
               smtp,
               port );
            return;
         }

         string from;
         if (!string.IsNullOrEmpty(from1))
         {
            from = from1;
         }
         else if (string.IsNullOrEmpty(from2))
         {
            from = from2;
         }
         else
         {
            System.Console.WriteLine("'From: ' not found in file {0}", args[0]);
            System.Console.WriteLine("and from no set");
            return;
         }

         patch_file.Close();
         System.Net.Mail.Attachment patch =
            new System.Net.Mail.Attachment(args[0]);

         patch.ContentType.MediaType = 
            System.Net.Mime.MediaTypeNames.Text.Plain;
         patch.ContentType.CharSet = "us-ascii";

         System.Net.Mail.MailMessage message =
            new System.Net.Mail.MailMessage(
               from, 
               to, 
               subject, 
               "See attachment");

         message.Attachments.Add( patch );

         m.Send(message);

         System.Console.WriteLine(
            "Send mail via smtp server '{0}', port '{1}', to '{2}', from '{3}' with subject '{4}'",
            smtp, port, to, from, subject );
      }
   }
}
