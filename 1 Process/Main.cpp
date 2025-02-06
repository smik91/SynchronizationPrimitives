#include <iostream>
#include <fstream>
#include <string>
#include <windows.h>

using namespace std;

struct Employee
{
    int id;
    char name[10];
    double workHours;
};

void replaceString(string& str, const string& stringWhichReplace, const string& stringInstead)
{
    if (stringWhichReplace.empty())
    {
        return;
    }

    size_t startPos = 0;
    while ((startPos = str.find(stringWhichReplace, startPos)) != string::npos)
    {
        str.replace(startPos, stringWhichReplace.length(), stringInstead);
        startPos += stringInstead.length();
    }
}

int main(int argc, char* argv[])
{
    string binaryFileName;
    int recordCount;


    cout << "Enter the name of the binary file: ";
    cin >> binaryFileName;
    cout << "Enter the number of records: ";
    cin >> recordCount;

    string appPath = argv[0]; // directory where all .exe files
    replaceString(appPath, "Main.exe", "");

    STARTUPINFOA startupInfo; 
    PROCESS_INFORMATION processInfo; 
    ZeroMemory(&startupInfo, sizeof(STARTUPINFOA)); 
    startupInfo.cb = sizeof(STARTUPINFOA);


    if (CreateProcessA((appPath + "Creator.exe").c_str(), 
        (LPSTR)((appPath + "Creator.exe " + binaryFileName + " " + to_string(recordCount)).c_str()), 
        NULL,
        NULL,
        FALSE,
        CREATE_NEW_CONSOLE,
        NULL, 
        NULL, 
        &startupInfo, 
        &processInfo))
    {
        WaitForSingleObject(processInfo.hProcess, INFINITE);
        CloseHandle(processInfo.hThread); 
        CloseHandle(processInfo.hProcess);
    }
    else
    {
        cerr << "Failed to start Creator.exe" << endl;
        return 1;
    }

    ifstream binaryFile(binaryFileName, ios::in | ios::binary);
    Employee employee;

    cout << "Contents of the binary file:" << endl;
    while (binaryFile.read(reinterpret_cast<char*>(&employee), sizeof(employee)))
    {
        cout << employee.id << " " << employee.name << " " << employee.workHours << "\n";
    }
    binaryFile.close();

    string reportFileName;
    double salary;

    cout << endl << "Enter the name of the report file: ";
    cin >> reportFileName;
    cout << "Enter the salary: ";
    cin >> salary;

    ZeroMemory(&startupInfo, sizeof(STARTUPINFOA));
    if (CreateProcessA((appPath + "Reporter.exe").c_str(),
        (LPSTR)((appPath + "Reporter.exe " + binaryFileName + " " + reportFileName + " " + to_string(salary)).c_str()),
        NULL, NULL, FALSE, CREATE_NEW_CONSOLE, NULL, NULL, &startupInfo, &processInfo))
    {
        WaitForSingleObject(processInfo.hProcess, INFINITE);
        CloseHandle(processInfo.hThread);
        CloseHandle(processInfo.hProcess);
    }
    else
    {
        cerr << "Failed to start Reporter.exe" << endl;
        return 1;
    }

    ifstream reportFile(reportFileName);
    string line;

    if (reportFile.is_open())
    {
        cout << "Report contents:" << endl;
        while (getline(reportFile, line))
        {
            cout << line << endl;
        }
        reportFile.close();
    }
    else
    {
        cerr << "Unable to open report file: " << reportFileName << endl;
    }

    return 0;
}
