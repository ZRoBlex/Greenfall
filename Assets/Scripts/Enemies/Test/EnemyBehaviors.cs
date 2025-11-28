using UnityEngine;

public static class EnemyBehaviors
{
    // Caminar / Patrullar
    public static void EnterWalking(EnemyController enemy)
    {
        enemy.ChangeState(new WanderState());
    }

    // Asustado (mirar al jugador y retroceder)
    public static void EnterScared(EnemyController enemy, Transform player)
    {
        enemy.ChangeState(new PassiveObserveState(player));
    }

    // Persecución agresiva
    public static void EnterChasing(EnemyController enemy, Transform target)
    {
        enemy.ChangeState(new ChaseState(target));
    }

    // Seguir al jugador (modo reclutado)
    public static void EnterFollowingPlayer(EnemyController enemy, Transform player)
    {
        enemy.ChangeState(new FollowPlayerState(player));
    }

    // Estado nulo / detener IA
    public static void StopAI(EnemyController enemy)
    {
        enemy.ChangeState(null);
    }
}
