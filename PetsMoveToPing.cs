using Kitchen;
using KitchenMods;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace KitchenCatDisco
{
    public class PetsMoveToPing : FranchiseSystem, IModSystem
    {
        private const float CAT_DISCO_COOLDOWN = 1f;
        private const float CAT_DISCO_EFFECT_SQRDISTANCE = 9f;
        private const float CAT_DISCO_EFFECT_POS_SPREAD = 0.5f;

        private struct CCatDiscoCooldown : IModComponent { }

        private static EntityQuery WaitCatsQuery;
        private static EntityQuery PingsQuery;

        protected override void Initialise()
        {
            WaitCatsQuery = GetEntityQuery(new QueryHelper()
                                           .All(typeof(CGroupWait),
                                                typeof(CGroupMember))
                                           .None(typeof(CGroupStartLeaving))
                                           );
            PingsQuery = GetEntityQuery(new QueryHelper()
                                        .All(typeof(CPlayerPing),
                                             typeof(CPosition),
                                             typeof(CLifetime))
                                        );
            base.Initialise();
        }

        protected override void OnUpdate()
        {
            if (!Has<CCatDiscoCooldown>())
            {
                RoamPets roamPets = base.World.GetExistingSystem<RoamPets>();
                roamPets.Enabled = true;
                NativeArray<Entity> pings = PingsQuery.ToEntityArray(Allocator.TempJob);

                int count = pings.Length;

                if (count > 0)
                {
                    int index;
                    if (count > 1)
                        index = UnityEngine.Random.Range(0, count);
                    else
                        index = 0;

                    if (Require<CPosition>(pings[index], out CPosition cPosition))
                    {
                        Main.LogInfo("Meow!");
                        NativeArray<Entity> WaitCats = WaitCatsQuery.ToEntityArray(Allocator.TempJob);
                        for (int i = 0; i < WaitCats.Length; i++)
                        {
                            DynamicBuffer<CGroupMember> cats = EntityManager.GetBuffer<CGroupMember>(WaitCats[i]);

                            for (int j = 0; j < cats.Length; j++)
                            {
                                if (Require<CPosition>(cats[j], out CPosition cPosition2))
                                {
                                    Vector3 v = cPosition2.Position - cPosition.Position;
                                    if (!EntityManager.HasComponent<CMoveToLocation>(cats[j]))
                                    {
                                        EntityManager.AddComponent<CMoveToLocation>(cats[j]);
                                    }
                                    if (v.sqrMagnitude < CAT_DISCO_EFFECT_SQRDISTANCE)
                                    {

                                        EntityManager.SetComponentData(cats[j], new CMoveToLocation
                                        {
                                            Location = cPosition + UnityEngine.Random.insideUnitSphere.normalized * CAT_DISCO_EFFECT_POS_SPREAD,
                                            DesiredFacing =  UnityEngine.Random.insideUnitSphere.normalized,
                                            StoppingDistance = 0.5f
                                        });
                                    }
                                }
                            }
                        }
                        WaitCats.Dispose();
                        roamPets.Enabled = false;
                        StartCatDiscoCooldown();
                    }
                }
                pings.Dispose();
            }
        }

        private void StartCatDiscoCooldown()
        {
            Entity entity = EntityManager.CreateEntity();
            EntityManager.AddComponent<CCatDiscoCooldown>(entity);
            EntityManager.AddComponent<CLifetime>(entity);
            EntityManager.SetComponentData(entity, new CLifetime
            {
                RemainingLife = CAT_DISCO_COOLDOWN
            });
        }
    }
}
