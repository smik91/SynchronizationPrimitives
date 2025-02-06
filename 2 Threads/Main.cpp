#include <windows.h>
#include <iostream>
#include <vector>
#include <string>

using namespace std;

struct FunData {
    int minValue = 0;
    int maxValue = 0;
    int* array = nullptr;
    int arraySize = 0;
    double averageValue = 0;
};

DWORD WINAPI CalculateMinMax(LPVOID param) {
    FunData* data = (FunData*)param;
    cout << "Thread CalculateMinMax started\n";
    data->minValue = data->array[0];
    data->maxValue = data->array[0];
    for (int i = 1; i < data->arraySize; i++) {
        if (data->maxValue < data->array[i]) {
            data->maxValue = data->array[i];
        }
        Sleep(7);
        if (data->minValue > data->array[i]) {
            data->minValue = data->array[i];
        }
        Sleep(7);
    }
    cout << "Min in array: " << data->minValue << "\n";
    cout << "Max in array: " << data->maxValue << "\n";
    cout << "Thread CalculateMinMax finished\n";
    return 0;
}

DWORD WINAPI CalculateAverage(LPVOID param) {
    FunData* data = (FunData*)param;
    cout << "Thread CalculateAverage started\n";
    for (int i = 0; i < data->arraySize; i++) {
        data->averageValue += data->array[i];
        Sleep(12);
    }
    data->averageValue = data->averageValue / static_cast<double>(data->arraySize);
    cout << "Average value: " << data->averageValue << "\n";
    cout << "Thread CalculateAverage finished\n";
    return 0;
}

int main() {
    int arraySize;
    cout << "Enter the size of the array: ";
    cin >> arraySize;

    int* array = new int[arraySize];
    cout << "Fill the array: ";
    for (int i = 0; i < arraySize; i++) {
        cin >> array[i];
    }

    FunData* data = new FunData;
    data->array = array;
    data->arraySize = arraySize;

    HANDLE threadMinMax;
    DWORD threadIdMinMax;
    threadMinMax = CreateThread(
        NULL,
        0,
        CalculateMinMax,
        (LPVOID)data,
        0,
        &threadIdMinMax
    );
    if (threadMinMax == NULL) {
        return GetLastError();
    }

    HANDLE threadAverage;
    DWORD threadIdAverage;
    threadAverage = CreateThread(
        NULL,
        0,
        CalculateAverage,
        (LPVOID)data,
        0,
        &threadIdAverage
    );
    if (threadAverage == NULL) {
        return GetLastError();
    }

    WaitForSingleObject(threadMinMax, INFINITE);
    WaitForSingleObject(threadAverage, INFINITE);

    CloseHandle(threadMinMax);
    CloseHandle(threadAverage);

    for (int i = 0; i < arraySize; i++) {
        if (array[i] == data->maxValue) {
            array[i] = static_cast<int>(data->averageValue);
        }
        if (array[i] == data->minValue) {
            array[i] = static_cast<int>(data->averageValue);
        }
    }

    cout << "Modified array: ";
    for (int i = 0; i < arraySize; i++) {
        cout << array[i] << " ";
    }

    delete[] array;
    delete data;

    return 0;
}