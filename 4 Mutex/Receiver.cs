using System;
using System.Diagnostics;

namespace Reciever;

internal class Program
{
    const string MutexName = "FileMutex";
    const string MessagesAvailableSemaphoreName = "MessagesAvailableSemaphore";
    const string EmptySlotsSemaphoreName = "EmptySlotsSemaphore";
    const string SenderReadyEventNamePrefix = "SenderReadyEvent";

    static void Main()
    {
        try
        {
            Console.Write("Filename for messages: ");
            string fileName = Console.ReadLine();
            string pathToFile = @"ur_path_to_file" + fileName;
            Console.Write("Max amount of messages: ");
            int maxMessages = int.Parse(Console.ReadLine());

            using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
            }

            Console.Write("Amount Sender: ");
            int numberOfSenders = int.Parse(Console.ReadLine());

            bool createdNew;
            Mutex fileMutex = new Mutex(false, MutexName, out createdNew);

            Semaphore messagesAvailable = new Semaphore(0, maxMessages, MessagesAvailableSemaphoreName, out createdNew);
            Semaphore emptySlots = new Semaphore(maxMessages, maxMessages, EmptySlotsSemaphoreName, out createdNew);

            EventWaitHandle[] senderReadyEvents = new EventWaitHandle[numberOfSenders];
            for (int i = 0; i < numberOfSenders; i++)
            {
                string eventName = SenderReadyEventNamePrefix + i;
                senderReadyEvents[i] = new EventWaitHandle(false, EventResetMode.ManualReset, eventName, out createdNew);
            }

            for (int i = 0; i < numberOfSenders; i++)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = @"path_to_sender\Sender.exe";
                psi.Arguments = $"{fileName} {i}";
                psi.UseShellExecute = true;
                psi.CreateNoWindow = false;

                Process senderProcess = Process.Start(psi);
            }

            Console.WriteLine("Waiting all Senders"...);
            WaitHandle.WaitAll(senderReadyEvents);
            Console.WriteLine("All senders are ready.");

            while (true)
            {
                Console.WriteLine("1 for reading message, 0 for exit:");
                string input = Console.ReadLine();
                if (input == "1")
                {
                    messagesAvailable.WaitOne();
                    fileMutex.WaitOne();
                    string message = ReadMessage(fileName);
                    fileMutex.ReleaseMutex();
                    emptySlots.Release();
                    Console.WriteLine("Received message: " + message);
                }
                else if (input == "0")
                {
                    Console.WriteLine("Terminate Receiver.");
                    break;
                }
                else
                {
                    Console.WriteLine("Unhandled input.");
                }
            }

            messagesAvailable.Close();
            emptySlots.Close();
            fileMutex.Close();
            foreach (var ev in senderReadyEvents)
            {
                ev.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    static string ReadMessage(string fileName)
    {
        string message = "";
        List<string> messages = new List<string>();

        using (var reader = new StreamReader(fileName))
        {
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(line))
                {
                    messages.Add(line);
                }
            }
        }

        if (messages.Count > 0)
        {
            message = messages[0];
            messages.RemoveAt(0);

            using (var writer = new StreamWriter(fileName, false))
            {
                foreach (var msg in messages)
                {
                    writer.WriteLine(msg);
                }
            }
        }

        return message;
    }
}