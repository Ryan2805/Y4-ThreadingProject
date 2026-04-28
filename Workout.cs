using System;

namespace GymWorkoutTracker
{
    public class Workout
    {
        public string ExerciseName { get; set; }
        public string MuscleGroup { get; set; }
        public int Sets { get; set; }
        public int Reps { get; set; }

        public Workout()
        {

        }

        public Workout(string exerciseName, string muscleGroup, int sets, int reps)
        {
            ExerciseName = exerciseName;
            MuscleGroup = muscleGroup;
            Sets = sets;
            Reps = reps;
        }

        public override string ToString()
        {
            return $"{ExerciseName} - {MuscleGroup} - {Sets} sets x {Reps} reps";
        }
    }
}