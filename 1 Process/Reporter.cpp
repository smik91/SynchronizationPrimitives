#include <iostream>
#include <fstream>
#include <string>

using namespace std;

struct Employee
{
    int id;
    char name[10];
    double workHours;
};

int main(int argc, char* argv[])
{
    if (argc != 4)
    {
        cerr << "Usage: Reporter <binary_file> <report_file> <salary>" << endl;
        return 1;
    }

    string binaryFileName = argv[1];
    string reportFileName = argv[2];
    reportFileName = reportFileName + ".txt";
    double salaryRate = stod(argv[3]);

    ifstream binFile(binaryFileName, ios::binary);
    if (!binFile)
    {
        cerr << "Error opening binary file." << endl;
        return 1;
    }

    ofstream reportFile(reportFileName);
    if (!reportFile)
    {
        cerr << "Error creating report file." << endl;
        return 1;
    }

    Employee employeeRecord;
    while (binFile.read(reinterpret_cast<char*>(&employeeRecord), sizeof(employeeRecord)))
    {
        double payment = employeeRecord.workHours * salaryRate;
        reportFile << employeeRecord.id << " " << employeeRecord.name << " "
            << employeeRecord.workHours << " " << payment << "\n";
    }

    binFile.close();
    reportFile.close();
    return 0;
}
