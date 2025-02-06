using Common;
using System.IO.Pipes;
using System.Text.Json;

namespace ClientApp
{
    internal class Program
    {
        private static readonly string pipeName = "employee_pipe";

        private static async Task Main(string[] args)
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                try
                {
                    Console.WriteLine("Trying connect to the server...");
                    await pipeClient.ConnectAsync(5000);
                    Console.WriteLine("Connected successfully.");

                    var reader = new StreamReader(pipeClient);
                    var writer = new StreamWriter(pipeClient) { AutoFlush = true };

                    while (true)
                    {
                        Console.WriteLine("\nChose operation:");
                        Console.WriteLine("1. Reading");
                        Console.WriteLine("2. Modification");
                        Console.WriteLine("3. Exit");
                        Console.Write(">>>");
                        string? choiceInput = Console.ReadLine();
                        if (choiceInput == null)
                            continue;

                        if (!int.TryParse(choiceInput, out int choice))
                        {
                            Console.WriteLine("Invalid input. Try again.");
                            continue;
                        }

                        if (choice == 3)
                        {
                            Console.WriteLine("Exiting...");
                            break;
                        }

                        Console.Write("Write employee's ID: ");
                        string? idInput = Console.ReadLine();
                        if (!int.TryParse(idInput, out int id))
                        {
                            Console.WriteLine("Invalid ID. Try again.");
                            continue;
                        }

                        Request request = new Request
                        {
                            Action = (choice == 1) ? ActionType.Read : ActionType.Modify,
                            Employee = new Employee { Num = id }
                        };

                        string requestJson = JsonSerializer.Serialize(request);
                        await writer.WriteLineAsync(requestJson);

                        if (choice == 1)
                        {
                            string? responseJson = await reader.ReadLineAsync();
                            if (responseJson == null)
                            {
                                Console.WriteLine("Received invalid data.");
                                continue;
                            }

                            Response? response = JsonSerializer.Deserialize<Response>(responseJson);
                            if (response == null)
                            {
                                Console.WriteLine("Received invalid data.");
                                continue;
                            }

                            if (response.Success && response.Employee != null)
                            {
                                Console.WriteLine($"ID: {response.Employee.Num}, Name: {response.Employee.Name}, Hours: {response.Employee.Hours}");
                            }
                            else
                            {
                                Console.WriteLine($"Error: {response.Message}");
                            }
                        }
                        else if (choice == 2)
                        {
                            string? responseJson = await reader.ReadLineAsync();
                            if (responseJson == null)
                            {
                                Console.WriteLine("Received invalid data.");
                                continue;
                            }

                            Response? response = JsonSerializer.Deserialize<Response>(responseJson);
                            if (response == null)
                            {
                                Console.WriteLine("Received invalid data.");
                                continue;
                            }

                            if (response.Success && response.Employee != null)
                            {
                                Console.WriteLine($"RN Data: ID: {response.Employee.Num}, Name: {response.Employee.Name}, Hours: {response.Employee.Hours}");

                                Console.Write("New name: ");
                                string newName = Console.ReadLine()!;
                                if (string.IsNullOrWhiteSpace(newName))
                                    newName = response.Employee.Name;

                                Console.Write("New hours: ");
                                string? hoursInput = Console.ReadLine();
                                double newHours;
                                if (!double.TryParse(hoursInput, out newHours) || newHours < 0)
                                    newHours = response.Employee.Hours;

                                Employee updatedEmp = new Employee
                                {
                                    Num = response.Employee.Num,
                                    Name = newName,
                                    Hours = newHours
                                };

                                string updatedEmpJson = JsonSerializer.Serialize(updatedEmp);
                                await writer.WriteLineAsync(updatedEmpJson);

                                string? confirmationJson = await reader.ReadLineAsync();
                                if (confirmationJson == null)
                                {
                                    Console.WriteLine("Received invalid data.");
                                    continue;
                                }

                                Response? confirmation = JsonSerializer.Deserialize<Response>(confirmationJson);
                                if (confirmation == null)
                                {
                                    Console.WriteLine("Received invalid data.");
                                    continue;
                                }

                                if (confirmation.Success)
                                {
                                    Console.WriteLine("Data updated successfully.");
                                }
                                else
                                {
                                    Console.WriteLine($"Error: {confirmation.Message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"Error: {response.Message}");
                            }
                        }
                    }
                }
                catch (TimeoutException)
                {
                    Console.WriteLine("Unable connect to the server.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

            Console.WriteLine("Client was terminated.");
        }
    }
}