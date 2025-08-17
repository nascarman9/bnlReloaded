using BNLReloadedServer.BaseTypes;

namespace BNLReloadedServer.Database;

public class DummyPlayerDatabase : IPlayerDatabase
{
    private ulong _yourSteamId;
    public uint GetPlayerId(ulong steamId)
    {
        _yourSteamId = steamId;
        return 1;
    }

    public string GetAuthTokenForPlayer(uint playerId)
    {
        return playerId.GetHashCode().ToString();
    }

    public uint? GetPlayerIdFromAuthToken(string authToken)
    {
        return 1;
    }

    public ProfileData GetPlayerProfile(uint playerId)
    {
        return new ProfileData
        {
            Nickname = "testUser",
            SteamId = _yourSteamId,
            League = new League
            {
                Tier = 1,
                Division = 1,
                Points = 0,
                JoinedTime = default,
                LastPlayedTime = default,
                Status = null
            },
            Progression = new PlayerProgression
            {
                PlayerProgress = new XpInfo
                {
                    Level = 100,
                    LevelXp = 0,
                    XpForNextLevel = CatalogueHelper.PlayerXpForLevel(100)
                },
                HeroesProgress = new Dictionary<Key, XpInfo>
                {
                    {
                        Catalogue.Key("unit_hero_sarge_stone"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_abe"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_astro"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_boxer"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_cogwheel"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_djinn"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_doc_eliza"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_engineer"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_hunter"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_kira"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_magnus"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_ninja"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_roly"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_kreepy"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    },
                    {
                        Catalogue.Key("unit_hero_trondson"), new XpInfo
                        {
                            Level = 100,
                            LevelXp = 0,
                            XpForNextLevel = 500
                        }
                    }
                }
            },
            MatchHistory = [],
            HeroStats = [],
            GlobalStats = new GlobalStats(),
            SelectedBadges = new Dictionary<BadgeType, List<Key>>
            {
                { BadgeType.Border, [Catalogue.Key("badge_border_magnus_1")] },
                { BadgeType.Icon, [Catalogue.Key("badge_icon_community_representative")] },
                { BadgeType.Title, [Catalogue.Key("badge_title_hunter_the_most_dangerous_game")] }
            },
            LookingForFriends = false,
            FriendsCount = 0
        };
    }
}