using BridgeGym.Models;

namespace BridgeGym.Services;

public interface IExerciseService
{
    HandExerciseViewModel GenerateHand(int currentHand, int totalHands, ExerciseMode mode);
}
