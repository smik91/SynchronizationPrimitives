#include <windows.h>
#include <string>
#include <vector>
#include <map>
#include <iostream>
#include <fstream>
#include <sstream>

using namespace std;

struct DeviceStats
{
    int logCount;
    vector<string> actions;
};

map<string, DeviceStats> deviceStatistics;
CRITICAL_SECTION statsLock;
HANDLE fileMutex;
string logFilePath = "logs.txt";

struct ThreadParams
{
    string deviceName;
};

DWORD WINAPI DeviceThread(LPVOID lpParam)
{
    ThreadParams* params = (ThreadParams*)lpParam;
    string device = params->deviceName;
    delete params;

    ifstream file;
    string line;

    WaitForSingleObject(fileMutex, INFINITE);
    file.open(logFilePath);
    if (!file.is_open())
    {
        cerr << "Unable to open file: " << logFilePath << endl;
        ReleaseMutex(fileMutex);
        return 1;
    }

    while (getline(file, line))
    {
        size_t delimiterPos = line.find(':');
        if (delimiterPos != string::npos)
        {
            string currentDevice = line.substr(0, delimiterPos);
            if (currentDevice == device)
            {
                string action = line.substr(delimiterPos + 1);
                EnterCriticalSection(&statsLock);
                deviceStatistics[device].logCount++;
                deviceStatistics[device].actions.push_back(action);
                LeaveCriticalSection(&statsLock);
            }
        }
    }

    file.close();
    ReleaseMutex(fileMutex);

    return 0;
}

int main()
{
    InitializeCriticalSection(&statsLock);
    fileMutex = CreateMutex(NULL, FALSE, NULL);
    if (fileMutex == NULL)
    {
        cerr << "Unable to create mutex." << endl;
        return 1;
    }

    ifstream initialFile(logFilePath);
    if (!initialFile.is_open())
    {
        cerr << "Unable to open file: " << logFilePath << endl;
        return 1;
    }

    string line;
    vector<string> devices;
    while (getline(initialFile, line))
    {
        size_t delimiterPos = line.find(':');
        if (delimiterPos != string::npos)
        {
            string device = line.substr(0, delimiterPos);
            if (find(devices.begin(), devices.end(), device) == devices.end())
            {
                devices.push_back(device);
            }
        }
    }
    initialFile.close();

    vector<HANDLE> threads;
    for (const auto& device : devices)
    {
        ThreadParams* params = new ThreadParams();
        params->deviceName = device;
        HANDLE thread = CreateThread(
            NULL,
            0,
            DeviceThread,
            params,
            0,
            NULL
        );

        if (thread == NULL)
        {
            cerr << "Unable to create thread for devices: " << device << endl;
            delete params;
            continue;
        }
        threads.push_back(thread);
    }

    WaitForMultipleObjects(threads.size(), threads.data(), TRUE, INFINITE);

    for (auto& thread : threads)
    {
        CloseHandle(thread);
    }

    EnterCriticalSection(&statsLock);
    for (const auto& entry : deviceStatistics)
    {
        cout << "Device: " << entry.first << "\n";
        cout << "Log amount: " << entry.second.logCount << "\n";
        cout << "Actions:\n";
        for (const auto& action : entry.second.actions)
        {
            cout << "  - " << action << "\n";
        }
        cout << "--------------------------\n";
    }
    LeaveCriticalSection(&statsLock);

    CloseHandle(fileMutex);
    DeleteCriticalSection(&statsLock);

    return 0;
}