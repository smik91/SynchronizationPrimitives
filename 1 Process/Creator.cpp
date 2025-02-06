#include <iostream>
#include <fstream>
#include <string>

struct Employee
{
    int id;
    char name[10];
    double workHours;
};

using namespace std;

int main(int argc, char* argv[])
{
    if (argc != 3)
    {
        cerr << "Usage: Creator <filename> <number_of_records>" << endl;
        return 1;
    }

    string binaryFileName = argv[1];
    int recordCount = stoi(argv[2]);

    ofstream binFile(binaryFileName, ios::binary);
    if (!binFile)
    {
        cerr << "Error opening binary file." << endl;
        return 1;
    }

    Employee employeeRecord;

    for (int i = 0; i < recordCount; ++i)
    {
        employeeRecord.id = i + 1;

        cout << "Enter name for employee " << employeeRecord.id << ": ";
        cin >> employeeRecord.name;

        cout << "Enter hours for employee " << employeeRecord.id << ": ";
        cin >> employeeRecord.workHours;

        binFile.write(reinterpret_cast<char*>(&employeeRecord), sizeof(employeeRecord));
    }

    binFile.close();
    return 0;
}
