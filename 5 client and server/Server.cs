using Common;
using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

internal class Program
{
    static Dictionary<int, SemaphoreSlim> semaphores { get; set; } = new Dictionary<int, SemaphoreSlim>();
    static List<Employee> employees { get; set; } = new List<Employee>();
    static string fileName { get; set; }
    private static readonly string pipeName = "employee_pipe";

    private static async Task Main(string[] args)
    {
        Console.Write("Filename: ");
        fileName = Console.ReadLine()!;

        Console.Write("Employee amount: ");
        if (!int.TryParse(Console.ReadLine(), out int numberOfEmployees) || numberOfEmployees <= 0)
        {
            Console.WriteLine("Invalid amount.");
            return;
        }

        for (int i = 0; i < numberOfEmployees; i++)
        {
            Console.WriteLine($"Write employee data {i + 1}:");

            int id;
            while (true)
            {
                Console.Write("ID: ");
                if (int.TryParse(Console.ReadLine(), out id))
                    break;
                Console.WriteLine("Invalid ID. Try again.");
            }

            Console.Write("Name: ");
            string name = Console.ReadLine()!;

            double hours;
            while (true)
            {
                Console.Write("Hours: ");
                if (double.TryParse(Console.ReadLine(), out hours))
                    break;
                Console.WriteLine("Invalid hours. Try again.");
            }

            employees.Add(new Employee { Num = id, Name = name, Hours = hours });
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(employees, options);
        await File.WriteAllTextAsync(fileName, jsonString);

        Console.WriteLine("File:");
        Console.WriteLine(jsonString);

        foreach (var emp in employees)
        {
            semaphores[emp.Num] = new SemaphoreSlim(1, 1);
        }

        Console.Write("Write clients amount ");
        if (!int.TryParse(Console.ReadLine(), out int numberOfClients) || numberOfClients <= 0)
        {
            Console.WriteLine("Invalid amount.");
            return;
        }

        for (int i = 0; i < numberOfClients; i++)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = @"path_to_client_exe",
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(startInfo);
        }

        Task serverTask = Task.Run(() => ListenForClients());

        Console.WriteLine("Server is running.");
        Console.WriteLine("Press any key to shutdown server.");
        Console.ReadKey();

        foreach (var semaphore in semaphores.Values)
        {
            semaphore.Dispose();
        }
    }

    private static async Task ListenForClients()
    {
        while (true)
        {
            NamedPipeServerStream pipeServer = new NamedPipeServerStream(
                pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Message,
                PipeOptions.Asynchronous);

            try
            {
                await pipeServer.WaitForConnectionAsync();
                Console.WriteLine("Client connected.");

                _ = Task.Run(() => HandleClient(pipeServer));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during client connect: {ex.Message}");
                pipeServer.Dispose();
            }
        }
    }

    private static async Task HandleClient(NamedPipeServerStream pipe)
    {
        try
        {
            using (pipe)
            {
                var reader = new StreamReader(pipe);
                var writer = new StreamWriter(pipe) { AutoFlush = true };

                while (pipe.IsConnected)
                {
                    string? requestJson = await reader.ReadLineAsync();
                    if (requestJson == null)
                        break;

                    Request? request = JsonSerializer.Deserialize<Request>(requestJson);
                    if (request == null)
                        continue;

                    if (request.Action == ActionType.Read)
                    {
                        await HandleReadRequest(request, writer);
                    }
                    else if (request.Action == ActionType.Modify)
                    {
                        await HandleModifyRequest(request, reader, writer);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during client handling: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("Client disconnect.");
        }
    }

    private static async Task HandleReadRequest(Request request, StreamWriter writer)
    {
        if (!semaphores.ContainsKey(request.Employee.Num))
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(new Response { Success = false, Message = "Not found." }));
            return;
        }

        SemaphoreSlim semaphore = semaphores[request.Employee.Num];
        await semaphore.WaitAsync();
        try
        {
            Employee? emp = employees.Find(e => e.Num == request.Employee.Num);
            if (emp != null)
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new Response { Success = true, Employee = emp }));
            }
            else
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new Response { Success = false, Message = "Not found." }));
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static async Task HandleModifyRequest(Request request, StreamReader reader, StreamWriter writer)
    {
        if (!semaphores.ContainsKey(request.Employee.Num))
        {
            await writer.WriteLineAsync(JsonSerializer.Serialize(new Response { Success = false, Message = "Not found." }));
            return;
        }

        SemaphoreSlim semaphore = semaphores[request.Employee.Num];
        await semaphore.WaitAsync();
        try
        {
            Employee? emp = employees.FirstOrDefault(e => e.Num == request.Employee.Num);
            if (emp != null)
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new Response { Success = true, Employee = emp }));

                string? updatedEmpJson = await reader.ReadLineAsync();
                if (updatedEmpJson == null)
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(new Response { Success = false, Message = "Received incorrcect data." }));
                    return;
                }

                Employee? updatedEmp = JsonSerializer.Deserialize<Employee>(updatedEmpJson);
                if (updatedEmp == null)
                {
                    await writer.WriteLineAsync(JsonSerializer.Serialize(new Response { Success = false, Message = "Received incorrcect data." }));
                    return;
                }

                emp.Name = updatedEmp.Name;
                emp.Hours = updatedEmp.Hours;

                string updatedJson = JsonSerializer.Serialize(employees, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(fileName, updatedJson);

                await writer.WriteLineAsync(JsonSerializer.Serialize(new Response { Success = true, Message = "Data was updated successfully." }));
            }
            else
            {
                await writer.WriteLineAsync(JsonSerializer.Serialize(new Response { Success = false, Message = "Not found." }));
            }
        }
        finally
        {
            semaphore.Release();
        }
    }
}