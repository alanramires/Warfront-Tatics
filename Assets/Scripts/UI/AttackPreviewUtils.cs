using UnityEngine;

public static class AttackPreviewUtils
{
    public static void Show(UnitMovement attacker, UnitMovement target)
    {
        if (AttackPreviewLine.Instance == null) return;
        if (attacker == null || target == null) return;
        if (attacker.boardCursor == null || attacker.boardCursor.mainGrid == null) return;

        Grid grid = attacker.boardCursor.mainGrid;
        TrajectoryType traj = GetPrimaryTrajectory(attacker);
        AttackPreviewLine.Instance.Show(grid, attacker.currentCell, target.currentCell, traj);
    }

    public static TrajectoryType GetPrimaryTrajectory(UnitMovement attacker)
    {
        if (attacker != null && attacker.myWeapons != null && attacker.myWeapons.Count > 0)
        {
            var cfg = attacker.myWeapons[0];
            if (cfg.data != null) return cfg.data.trajectory;
        }
        return TrajectoryType.Straight;
    }
}