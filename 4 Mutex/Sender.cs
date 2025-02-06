namespace Sender;

internal class Program
{
    const string MutexName = "FileMutex";
    const string MessagesAvailableSemaphoreName = "MessagesAvailableSemaphore";
    const string EmptySlotsSemaphoreName = "EmptySlotsSemaphore";
    const string SenderReadyEventNamePrefix = "SenderReadyEvent";

    static void Main(string[] args)
    {
        try
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Using: Sender.exe <file name> <index Sender>");
                return;
            }

            string fileName = args[0];
            int senderIndex = int.Parse(args[1]);

            Mutex fileMutex = Mutex.OpenExisting(MutexName);
            Semaphore messagesAvailable = Semaphore.OpenExisting(MessagesAvailableSemaphoreName);
            Semaphore emptySlots = Semaphore.OpenExisting(EmptySlotsSemaphoreName);

            string eventName = SenderReadyEventNamePrefix + senderIndex;
            EventWaitHandle senderReadyEvent = EventWaitHandle.OpenExisting(eventName);

            senderReadyEvent.Set();
            senderReadyEvent.Close();

            while (true)
            {
                Console.WriteLine("1 for send message, 0 for exit:");
                string input = Console.ReadLine();
                if (input == "1")
                {
                    emptySlots.WaitOne();
                    fileMutex.WaitOne();

                    Console.Write("Write message: ");
                    string message = Console.ReadLine();
                    if (message.Length > 20)
                    {
                        message = message.Substring(0, 20);
                    }

                    using (var writer = new StreamWriter(fileName, true))
                    {
                        writer.WriteLine(message);
                    }

                    fileMutex.ReleaseMutex();
                    messagesAvailable.Release();
                }
                else if (input == "0")
                {
                    Console.WriteLine("Terminating Sender.");
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
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            Console.WriteLine("Error: Failed to open synchronization objects. Make sure Receiver is running.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }
}
