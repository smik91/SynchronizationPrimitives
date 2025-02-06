#include <Windows.h>
#include <iostream>

using namespace std;

int arraySize = 0;
int* sharedArray = nullptr;

CRITICAL_SECTION criticalSection;
HANDLE* threadHandles;
HANDLE* threadStartEvents;
HANDLE* threadStopEvents;
HANDLE* threadExitEvents;
HANDLE mutexHandle;

DWORD WINAPI MarkerFunction(LPVOID threadIndex)
{
    WaitForSingleObject(threadStartEvents[(int)threadIndex], INFINITE);
    int markedCount = 0;
    srand((int)threadIndex);

    while (true) {
        EnterCriticalSection(&criticalSection);
        int randomIndex = rand() % arraySize;

        if (sharedArray[randomIndex] == 0) {
            Sleep(5);
            sharedArray[randomIndex] = (int)threadIndex + 1;
            markedCount++;
            Sleep(5);
            LeaveCriticalSection(&criticalSection);
        }
        else {
            cout << "Thread: " << (int)threadIndex + 1 << "\n";
            cout << "Marked elements: " << markedCount << "\n";
            cout << "Index of element, which cannot be marked: " << randomIndex << "\n";
            LeaveCriticalSection(&criticalSection);

            SetEvent(threadStopEvents[(int)threadIndex]);
            ResetEvent(threadStartEvents[(int)threadIndex]);

            HANDLE events[] = { threadStartEvents[(int)threadIndex], threadExitEvents[(int)threadIndex] };
            if (WaitForMultipleObjects(2, events, FALSE, INFINITE) == WAIT_OBJECT_0 + 1) {
                EnterCriticalSection(&criticalSection);
                for (size_t i = 0; i < arraySize; i++) {
                    if (sharedArray[i] == (int)threadIndex + 1) {
                        sharedArray[i] = 0;
                    }
                }
                LeaveCriticalSection(&criticalSection);
                ExitThread(0);
            }
            else {
                ResetEvent(threadStopEvents[(int)threadIndex]);
                continue;
            }
        }
    }
}

int main()
{
    int threadCount = 0;

    cout << "Array size: ";
    cin >> arraySize;
    sharedArray = new int[arraySize] {};

    cout << "Threads count: ";
    cin >> threadCount;

    InitializeCriticalSection(&criticalSection);
    threadHandles = new HANDLE[threadCount];
    threadStartEvents = new HANDLE[threadCount];
    threadStopEvents = new HANDLE[threadCount];
    threadExitEvents = new HANDLE[threadCount];
    mutexHandle = CreateMutex(NULL, FALSE, NULL);

    for (int i = 0; i < threadCount; i++) {
        threadHandles[i] = CreateThread(NULL, 0, MarkerFunction, (LPVOID)i, 0, NULL);
        threadStartEvents[i] = CreateEvent(NULL, TRUE, FALSE, NULL);
        threadStopEvents[i] = CreateEvent(NULL, TRUE, FALSE, NULL);
        threadExitEvents[i] = CreateEvent(NULL, TRUE, FALSE, NULL);
    }

    for (int i = 0; i < threadCount; i++) {
        SetEvent(threadStartEvents[i]);
    }

    int completedThreads = 0;
    bool* threadExited = new bool[threadCount] {};

    while (completedThreads < threadCount) {
        WaitForMultipleObjects(threadCount, threadStopEvents, TRUE, INFINITE);

        cout << "Array right now: ";
        for (int i = 0; i < arraySize; i++) {
            cout << sharedArray[i] << " ";
        }
        cout << "\n";

        int stopThreadId;
        cout << "Number of thread which needs to be stopped: ";
        cin >> stopThreadId;
        stopThreadId--;

        if (!threadExited[stopThreadId]) {
            completedThreads++;
            threadExited[stopThreadId] = true;
            SetEvent(threadExitEvents[stopThreadId]);
            WaitForSingleObject(threadHandles[stopThreadId], INFINITE);

            CloseHandle(threadHandles[stopThreadId]);
            CloseHandle(threadExitEvents[stopThreadId]);
            CloseHandle(threadStartEvents[stopThreadId]);
        }

        cout << "Array after stopping thread: ";
        for (int i = 0; i < arraySize; i++) {
            cout << sharedArray[i] << " ";
        }
        cout << "\n";

        for (int i = 0; i < threadCount; i++) {
            if (!threadExited[i]) {
                ResetEvent(threadStopEvents[i]);
                SetEvent(threadStartEvents[i]);
            }
        }
    }

    DeleteCriticalSection(&criticalSection);
    CloseHandle(mutexHandle);
    delete[] sharedArray;
    delete[] threadHandles;
    delete[] threadStartEvents;
    delete[] threadStopEvents;
    delete[] threadExitEvents;
    delete[] threadExited;

    return 0;
}