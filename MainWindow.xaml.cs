/* Module: Advanced Programming Project
 * Name: Ryan Daly
 * Student Number: S00237889
 * Project: Gym Workout Tracker
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.IsolatedStorage;
using System.Threading;
using System.Windows;

namespace GymWorkoutTracker
{
    public partial class MainWindow : Window
    {
        private readonly List<Workout> workouts = new List<Workout>();
        private readonly Queue<string> searchMuscleGroups = new Queue<string>();

        private readonly object workoutLock = new object();
        private readonly object searchLock = new object();

        private Thread workoutThread;
        private Thread searchThread;
        private Thread storageThread;

        private readonly BackgroundWorker reportWorker = new BackgroundWorker();

        private bool appRunning = true;

        [ThreadStatic]
        private static int threadCounter;

        public MainWindow()
        {
            InitializeComponent();

            SetupBackgroundWorker();
            StartThreads();
        }

        private void SetupBackgroundWorker()
        {
            reportWorker.WorkerReportsProgress = true;
            reportWorker.WorkerSupportsCancellation = true;

            reportWorker.DoWork += ReportWorker_DoWork;
            reportWorker.ProgressChanged += ReportWorker_ProgressChanged;
            reportWorker.RunWorkerCompleted += ReportWorker_RunWorkerCompleted;
        }

        private void StartThreads()
        {
            
            // THREAD 1: Workout Record Thread
            
            workoutThread = new Thread(WorkoutRecordThreadMethod);
            workoutThread.Name = "Workout Record Thread";
            workoutThread.Priority = ThreadPriority.AboveNormal;
            workoutThread.IsBackground = true;

            
             // THREAD 2  Search Criteria Thread
            
            searchThread = new Thread(SearchCriteriaThreadMethod);
            searchThread.Name = "Search Criteria Thread";
            searchThread.Priority = ThreadPriority.Normal;
            searchThread.IsBackground = true;

            
             //THREAD 3: Isolated Storage Thread
             
            storageThread = new Thread(StorageThreadMethod);
            storageThread.Name = "Isolated Storage Thread";
            storageThread.Priority = ThreadPriority.BelowNormal;
            storageThread.IsBackground = true;

            workoutThread.Start();
            searchThread.Start();
            storageThread.Start("workouts.txt");

            
              //THREAD 4 BackgroundWorker Report Thread
             
            if (!reportWorker.IsBusy)
            {
                reportWorker.RunWorkerAsync();
            }

            txtGeneralStatus.Text = "Threads started successfully.";
        }

        
        private void WorkoutRecordThreadMethod()
        {
            while (appRunning)
            {
                threadCounter++;

                for (int seconds = 10; seconds >= 1; seconds--)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtWorkoutCountdown.Text = $"Next save in: {seconds}s";
                    });

                    Thread.Sleep(1000);
                }

                string exerciseName = "";
                string muscleGroup = "";
                string setsText = "";
                string repsText = "";

                Dispatcher.Invoke(() =>
                {
                    exerciseName = txtExerciseName.Text.Trim();
                    muscleGroup = txtMuscleGroup.Text.Trim();
                    setsText = txtSets.Text.Trim();
                    repsText = txtReps.Text.Trim();
                });

                if (!string.IsNullOrWhiteSpace(exerciseName) &&
                    !string.IsNullOrWhiteSpace(muscleGroup) &&
                    int.TryParse(setsText, out int sets) &&
                    int.TryParse(repsText, out int reps))
                {
                    Workout newWorkout = new Workout(exerciseName, muscleGroup, sets, reps);

                    
                    Monitor.Enter(workoutLock);
                    try
                    {
                        workouts.Add(newWorkout);
                        Monitor.Pulse(workoutLock);
                    }
                    finally
                    {
                        Monitor.Exit(workoutLock);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        txtExerciseName.Clear();
                        txtMuscleGroup.Clear();
                        txtSets.Clear();
                        txtReps.Clear();

                        txtWorkoutThreadStatus.Text = $"Saved workout: {newWorkout.ExerciseName}";
                        txtGeneralStatus.Text =
                            $"Workout added by {Thread.CurrentThread.Name}. Thread counter: {threadCounter}";
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtWorkoutThreadStatus.Text = "Waiting for complete workout input...";
                    });
                }
            }
        }

        
        private void SearchCriteriaThreadMethod()
        {
            while (appRunning)
            {
                string searchText = "";

                Dispatcher.Invoke(() =>
                {
                    searchText = txtSearchMuscle.Text.Trim();
                });

                for (int seconds = 5; seconds >= 1; seconds--)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtSearchCountdown.Text = $"Next check in: {seconds}s";
                    });

                    Thread.Sleep(1000);
                }

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    
                    Monitor.Enter(searchLock);
                    try
                    {
                        searchMuscleGroups.Enqueue(searchText);
                        Monitor.Pulse(searchLock);
                    }
                    finally
                    {
                        Monitor.Exit(searchLock);
                    }

                    Dispatcher.Invoke(() =>
                    {
                        txtSearchMuscle.Clear();
                        txtSearchThreadStatus.Text = $"Queued search: {searchText}";
                        txtGeneralStatus.Text = $"Search queued by {Thread.CurrentThread.Name}";
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtSearchThreadStatus.Text = "Waiting for valid search input...";
                    });
                }
            }
        }

        
        private void StorageThreadMethod(object fileNameObject)
        {
            string fileName = fileNameObject.ToString();

            while (appRunning)
            {
               
                Monitor.Enter(workoutLock);
                try
                {
                    Monitor.Wait(workoutLock, 15000);
                    SaveWorkoutsToIsolatedStorage(fileName);
                }
                finally
                {
                    Monitor.Exit(workoutLock);
                }

                Thread.Sleep(1000);
            }
        }

        
        private void SaveWorkoutsToIsolatedStorage(string fileName)
        {
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    using (IsolatedStorageFileStream stream =
                           new IsolatedStorageFileStream(fileName, FileMode.Create, store))
                    {
                        using (StreamWriter writer = new StreamWriter(stream))
                        {
                            foreach (Workout workout in workouts)
                            {
                                writer.WriteLine($"{workout.ExerciseName},{workout.MuscleGroup},{workout.Sets},{workout.Reps}");
                            }
                        }
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    txtGeneralStatus.Text = "Workouts saved to Isolated Storage.";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    txtGeneralStatus.Text = $"Storage error: {ex.Message}";
                });
            }
        }

        
        private void LoadWorkoutsFromIsolatedStorage()
        {
            try
            {
                lstResults.Items.Clear();

                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForAssembly())
                {
                    if (!store.FileExists("workouts.txt"))
                    {
                        lstResults.Items.Add("No isolated storage file found.");
                        return;
                    }

                    using (IsolatedStorageFileStream stream =
                           new IsolatedStorageFileStream("workouts.txt", FileMode.Open, store))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string line;

                            while ((line = reader.ReadLine()) != null)
                            {
                                lstResults.Items.Add(line);
                            }
                        }
                    }
                }

                txtGeneralStatus.Text = "Loaded workouts from Isolated Storage.";
            }
            catch (Exception ex)
            {
                txtGeneralStatus.Text = $"Load error: {ex.Message}";
            }
        }

        
        private void ReportWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (appRunning)
            {
                string muscleToSearch = null;

               
                Monitor.Enter(searchLock);
                try
                {
                    if (searchMuscleGroups.Count == 0)
                    {
                        Monitor.Wait(searchLock, 5000);
                    }

                    if (searchMuscleGroups.Count > 0)
                    {
                        muscleToSearch = searchMuscleGroups.Dequeue();
                    }
                }
                finally
                {
                    Monitor.Exit(searchLock);
                }

                if (!string.IsNullOrWhiteSpace(muscleToSearch))
                {
                    List<string> matchingWorkouts = new List<string>();

                    
                    Monitor.Enter(workoutLock);
                    try
                    {
                        foreach (Workout workout in workouts)
                        {
                            if (workout.MuscleGroup.Equals(muscleToSearch, StringComparison.OrdinalIgnoreCase))
                            {
                                matchingWorkouts.Add(workout.ToString());
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(workoutLock);
                    }

                    worker.ReportProgress(0, new SearchProgressData
                    {
                        ClearResults = true,
                        Message = $"Searching workouts for {muscleToSearch}..."
                    });

                    if (matchingWorkouts.Count == 0)
                    {
                        Thread.Sleep(500);

                        worker.ReportProgress(100, new SearchProgressData
                        {
                            ResultToAdd = $"No workouts found for {muscleToSearch}",
                            Message = $"Search complete for {muscleToSearch}"
                        });

                        Thread.Sleep(2000);

                        worker.ReportProgress(0, new SearchProgressData
                        {
                            ResetProgressBar = true
                        });

                        continue;
                    }

                    for (int i = 0; i < matchingWorkouts.Count; i++)
                    {
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        int percentage = ((i + 1) * 100) / matchingWorkouts.Count;

                        worker.ReportProgress(percentage, new SearchProgressData
                        {
                            ResultToAdd = matchingWorkouts[i],
                            Message = $"Displaying results for {muscleToSearch}..."
                        });

                        Thread.Sleep(1000);
                    }

                    Thread.Sleep(2000);

                    worker.ReportProgress(0, new SearchProgressData
                    {
                        ResetProgressBar = true,
                        Message = $"Finished search for {muscleToSearch}"
                    });
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        private void ReportWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBarSearch.Value = e.ProgressPercentage;

            if (e.UserState is SearchProgressData data)
            {
                if (data.ClearResults)
                {
                    lstResults.Items.Clear();
                }

                if (!string.IsNullOrWhiteSpace(data.ResultToAdd))
                {
                    lstResults.Items.Add(data.ResultToAdd);
                }

                if (!string.IsNullOrWhiteSpace(data.Message))
                {
                    txtGeneralStatus.Text = data.Message;
                }

                if (data.ResetProgressBar)
                {
                    progressBarSearch.Value = 0;
                }
            }
        }

        private void ReportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                txtGeneralStatus.Text = "Reporting worker cancelled.";
            }
            else if (e.Error != null)
            {
                txtGeneralStatus.Text = $"Reporting worker error: {e.Error.Message}";
            }
            else
            {
                txtGeneralStatus.Text = "Reporting worker completed.";
            }
        }

        private void btnLoadStorage_Click(object sender, RoutedEventArgs e)
        {
            LoadWorkoutsFromIsolatedStorage();
        }

        private void btnClearResults_Click(object sender, RoutedEventArgs e)
        {
            lstResults.Items.Clear();
            progressBarSearch.Value = 0;
            txtGeneralStatus.Text = "Results cleared.";
        }

        
        private void btnShowThreadInfo_Click(object sender, RoutedEventArgs e)
        {
            lstResults.Items.Clear();

            lstResults.Items.Add("=== THREAD INFORMATION ===");

            lstResults.Items.Add($"Name: {workoutThread.Name}");
            lstResults.Items.Add($"Priority: {workoutThread.Priority}");
            lstResults.Items.Add($"Is Background: {workoutThread.IsBackground}");
            lstResults.Items.Add($"Thread State: {workoutThread.ThreadState}");

            lstResults.Items.Add("");

            lstResults.Items.Add($"Name: {searchThread.Name}");
            lstResults.Items.Add($"Priority: {searchThread.Priority}");
            lstResults.Items.Add($"Is Background: {searchThread.IsBackground}");
            lstResults.Items.Add($"Thread State: {searchThread.ThreadState}");

            lstResults.Items.Add("");

            lstResults.Items.Add($"Name: {storageThread.Name}");
            lstResults.Items.Add($"Priority: {storageThread.Priority}");
            lstResults.Items.Add($"Is Background: {storageThread.IsBackground}");
            lstResults.Items.Add($"Thread State: {storageThread.ThreadState}");

            txtGeneralStatus.Text = "Thread information displayed.";
        }

   
        protected override void OnClosing(CancelEventArgs e)
        {
            appRunning = false;

            if (reportWorker.IsBusy)
            {
                reportWorker.CancelAsync();
            }

            Monitor.Enter(workoutLock);
            try
            {
                Monitor.PulseAll(workoutLock);
            }
            finally
            {
                Monitor.Exit(workoutLock);
            }

            Monitor.Enter(searchLock);
            try
            {
                Monitor.PulseAll(searchLock);
            }
            finally
            {
                Monitor.Exit(searchLock);
            }

            if (workoutThread != null && workoutThread.IsAlive)
            {
                workoutThread.Join(500);
            }

            if (searchThread != null && searchThread.IsAlive)
            {
                searchThread.Join(500);
            }

            if (storageThread != null && storageThread.IsAlive)
            {
                storageThread.Join(500);
            }

            base.OnClosing(e);
        }
    }
}