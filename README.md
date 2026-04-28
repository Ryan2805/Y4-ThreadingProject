#  Gym Workout Tracker (Advanced Programming Project)

##  Overview
This is a multithreaded WPF application developed as part of an Advanced Programming module.  
The app allows users to record workouts, search by muscle group, and store data using Isolated Storage.

---

##  Features
- Record workouts (exercise, muscle group, sets, reps)
- Search workouts by muscle group
- Save and load data using Isolated Storage
- Real-time UI updates using multithreading
- Thread information display

---

##  Multithreading
This project uses multiple threads that interact safely using `Monitor`:

- **Workout Thread** – Handles recording workouts
- **Search Thread** – Queues search requests
- **Storage Thread** – Saves data to isolated storage
- **BackgroundWorker** – Processes search results and updates UI

Thread features used:
- Thread naming & priority
- Background threads
- `Thread.Sleep()`
- `Thread.Join()`
- Parameter passing to threads
- `[ThreadStatic]` variables
- `Monitor.Enter / Exit / Wait / Pulse / PulseAll`

---

##  Isolated Storage
- Saves workout data to `workouts.txt`
- Loads stored data back into the UI
- Uses user-level isolated storage for the assembly

---

##  Technologies
- C# (.NET)
- WPF
- Multithreading (System.Threading)
- Isolated Storage

---

##  How to Run
1. Open the solution in Visual Studio  
2. Build the project  
3. Run the application  

---

##  Author
**Ryan Daly**  
Student Number: S00237889  

---
