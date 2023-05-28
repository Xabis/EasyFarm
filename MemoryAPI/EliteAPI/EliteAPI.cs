//Decompiled from EliteAPI .Net Wrapper

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace EliteMMO.API
{
    public class EliteAPI
    {
        public class AuctionHouseTools
        {
            private readonly IntPtr _instance;

            public int ItemCount => GetAHItemCount(_instance);

            public int ItemCountLoaded => GetAHItemCountLoaded(_instance);

            public bool IsDoneLoading => IsAHDoneLoadingItems(_instance);

            public AuctionHouseTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public List<int> GetItemIds()
            {
                List<int> list = new List<int>();
                int itemCount = ItemCount;
                if (itemCount == 0 || !IsDoneLoading)
                {
                    return list;
                }

                byte[] array = new byte[itemCount * 4];
                if (!GetAHItemIds(_instance, array))
                {
                    return list;
                }

                for (int i = 0; i < itemCount; i++)
                {
                    list.Add(BitConverter.ToInt32(array, 4 * i));
                }

                return list;
            }
        }

        public class AutoFollowTools
        {
            private readonly IntPtr _instance;

            public bool IsAutoFollowing
            {
                get
                {
                    return IsAutoFollowing(_instance);
                }
                set
                {
                    SetAutoFollow(_instance, value);
                }
            }

            public uint TargetIndex => GetAutoFollowTargetIndex(_instance);

            public uint TargetId => GetAutoFollowTargetId(_instance);

            public uint FollowIndex => GetAutoFollowFollowIndex(_instance);

            public uint FollowId => GetAutoFollowFollowId(_instance);

            public AutoFollowTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public bool SetAutoFollowInfo(uint targetIndex, uint targetId, uint followIndex, uint followId)
            {
                return EliteAPI.SetAutoFollowInfo(_instance, targetIndex, targetId, followIndex, followId);
            }

            public bool SetAutoFollowCoords(float fX, float fY, float fZ)
            {
                return EliteAPI.SetAutoFollowCoords(_instance, fX, fY, fZ);
            }
        }

        public class CastBarTools
        {
            private readonly IntPtr _instance;

            public float Max => GetCastBarMax(_instance);

            public float Count => GetCastBarCount(_instance);

            public float Percent => GetCastBarPercent(_instance);

            public CastBarTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }
        }

        public class ChatEntry
        {
            public int ChatType { get; set; }

            public Color ChatColor { get; set; }

            public int Index1 { get; set; }

            public int Index2 { get; set; }

            public int Length { get; set; }

            public DateTime Timestamp { get; set; }

            public string RawLine { get; set; }

            public string RawText { get; set; }

            public string Text { get; set; }
        }

        public class ChatTools
        {
            private readonly IntPtr _instance;

            private readonly ConcurrentQueue<ChatEntry> _chatLog = new ConcurrentQueue<ChatEntry>();

            private int _lastIndex;

            public int LineCount => GetChatLineCount(_instance);

            public ChatTools(IntPtr apiObject)
            {
                _instance = apiObject;
                _lastIndex = -1;
            }

            public string GetChatLineRaw(int index)
            {
                byte[] array = new byte[2048];
                EliteAPI.GetChatLineRaw(_instance, index, array, 2048);
                return Encoding.GetEncoding(1252).GetString(array).Trim(default(char));
            }

            public ChatEntry GetChatLine(int index)
            {
                string text = GetChatLineRaw(index).Trim(default(char));
                if (string.IsNullOrEmpty(text))
                {
                    return null;
                }

                string[] array = text.Split(new char[1] { ',' }, 22, StringSplitOptions.None);
                if (array.Length != 22)
                {
                    return new ChatEntry
                    {
                        RawLine = text,
                        RawText = "ERROR: FantasyFisher failed to parse the chat log properly!",
                        Text = "ERROR: FantasyFisher failed to parse the chat log properly!",
                        ChatColor = Color.Red,
                        ChatType = -1,
                        Index1 = 0,
                        Index2 = 0,
                        Length = 0,
                        Timestamp = DateTime.Now
                    };
                }

                ChatEntry chatEntry = new ChatEntry
                {
                    Timestamp = DateTime.Now,
                    RawLine = text,
                    RawText = array[21],
                    Text = CleanChatLine(array[21])
                };
                try
                {
                    chatEntry.ChatType = short.Parse(array[0], NumberStyles.AllowHexSpecifier);
                }
                catch (Exception)
                {
                    chatEntry.ChatType = -1;
                }

                try
                {
                    int argb = int.Parse(array[3], NumberStyles.AllowHexSpecifier);
                    chatEntry.ChatColor = Color.FromArgb(argb);
                }
                catch
                {
                    chatEntry.ChatColor = Color.Empty;
                }

                try
                {
                    chatEntry.Index1 = int.Parse(array[4], NumberStyles.AllowHexSpecifier);
                    chatEntry.Index2 = int.Parse(array[5], NumberStyles.AllowHexSpecifier);
                    return chatEntry;
                }
                catch
                {
                    chatEntry.Index1 = 0;
                    chatEntry.Index2 = 0;
                    return chatEntry;
                }
            }

            public ChatEntry GetNextChatLine()
            {
                UpdateInternalLog();
                if (_chatLog.Count == 0)
                {
                    return null;
                }

                ChatEntry line;
                if (_chatLog.TryDequeue(out line))
                    return line;
                return null;
            }

            private void UpdateInternalLog()
            {
                Stack<ChatEntry> stack = new Stack<ChatEntry>();
                if (_lastIndex < 0)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        ChatEntry chatLine = GetChatLine(i);
                        if (chatLine != null)
                        {
                            if (i == 0)
                            {
                                _lastIndex = chatLine.Index2;
                            }

                            stack.Push(chatLine);
                        }
                    }
                }
                else
                {
                    int num = 0;
                    bool flag = true;
                    for (int j = 0; j < 50; j++)
                    {
                        ChatEntry chatLine2 = GetChatLine(j);
                        if (chatLine2 != null && chatLine2.ChatType != -1)
                        {
                            if (num == 0)
                            {
                                num = chatLine2.Index2;
                            }

                            if (chatLine2.Index2 <= _lastIndex)
                            {
                                flag = false;
                                break;
                            }

                            stack.Push(chatLine2);
                        }
                    }

                    _lastIndex = num;
                    if (flag)
                    {
                        for (int k = LineCount - 50; k < 0; k++)
                        {
                            ChatEntry chatLine3 = GetChatLine(k);
                            if (chatLine3 != null && chatLine3.ChatType != -1)
                            {
                                if (chatLine3.Index2 <= _lastIndex)
                                {
                                    break;
                                }

                                stack.Push(chatLine3);
                            }
                        }
                    }
                }

                while (0 < stack.Count)
                {
                    _chatLog.Enqueue(stack.Pop());
                }
            }

            public static string CleanChatLine(string line)
            {
                try
                {
                    List<byte> list = new List<byte>();
                    byte[] bytes = Encoding.GetEncoding(1252).GetBytes(line);
                    int num = bytes.Length;
                    for (int num2 = 0; num2 < num; num2++)
                    {
                        while (num2 < num)
                        {
                            if ((bytes[num2] == 30 || bytes[num2] == 31) && num2 + 2 <= num)
                            {
                                num2 += 2;
                                continue;
                            }

                            if (bytes[num2] == 239 && num2 + 2 <= num)
                            {
                                if (bytes[num2 + 1] == 39)
                                {
                                    list.Add(123);
                                }

                                if (bytes[num2 + 1] == 40)
                                {
                                    list.Add(125);
                                }

                                num2 += 2;
                                continue;
                            }

                            if ((bytes[num2] == 127 && num2 + 2 <= num && bytes[num2 + 1] == 49) || (bytes[num2] == 129 && num2 + 2 <= num && bytes[num2 + 1] == 161) || (bytes[num2] == 129 && num2 + 2 <= num && bytes[num2 + 1] == 64) || (bytes[num2] == 135 && num2 + 2 <= num && bytes[num2 + 1] == 178) || (bytes[num2] == 135 && num2 + 2 <= num && bytes[num2 + 1] == 179))
                            {
                                num2 += 2;
                                continue;
                            }

                            if (num2 + 2 <= num && (bytes[num2 + 1] == 252 || bytes[num2 + 1] == 251))
                            {
                                num2 += 2;
                                continue;
                            }

                            if (bytes[num2] == 13 || bytes[num2] == 10 || bytes[num2] == 7)
                            {
                                list.Add(32);
                                num2++;
                                continue;
                            }

                            goto IL_017b;
                        }

                        break;
                    IL_017b:
                        if (num2 < num && bytes[num2] != 0)
                        {
                            list.Add(bytes[num2]);
                        }
                    }

                    list.Add(0);
                    string text = Encoding.GetEncoding(932).GetString(list.ToArray(), 0, list.Count);
                    Match match = Regex.Match(text, "^((?:\\[)?([0-9]+):([0-9]+):([0-9]+)(?:\\])?(\\s[aAmMpP][aAmMpP])?(?:\\])?)");
                    if (match.Success)
                    {
                        text = text.Remove(match.Index, match.Length).Trim();
                    }

                    return text.TrimEnd(default(char));
                }
                catch
                {
                    return line;
                }
            }
        }

        public class CraftItem
        {
            public byte Index { get; set; }

            public ushort ItemId { get; set; }

            public byte Count { get; set; }
        }

        public class CraftMenuTools
        {
            private readonly IntPtr _instance;

            public bool IsCraftMenuOpen => IsCraftMenuOpen(_instance);

            public bool IsCrafting => IsCrafting(_instance);

            public CraftMenuTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public CraftItem GetCraftItem(int index)
            {
                return new CraftItem
                {
                    Index = GetCraftItemIndex(_instance, index),
                    ItemId = GetCraftItemId(_instance, index),
                    Count = GetCraftItemCount(_instance, index)
                };
            }

            public List<CraftItem> GetCraftItems()
            {
                List<CraftItem> list = new List<CraftItem>();
                for (int i = 0; i < 8; i++)
                {
                    list.Add(GetCraftItem(i));
                }

                return list;
            }

            public bool SetCraftItem(int index, ushort itemId, byte itemIndex, byte itemCount)
            {
                return EliteAPI.SetCraftItem(_instance, index, itemId, itemIndex, itemCount);
            }
        }

        public class DialogInfo
        {
            public string Question { get; set; }

            public List<string> Options { get; set; }

            public string RawDialog { get; set; }

            public DialogInfo(string rawDialog)
            {
                RawDialog = rawDialog;
                Options = new List<string>();
                if (string.IsNullOrEmpty(rawDialog))
                {
                    Question = string.Empty;
                    return;
                }

                int num = rawDialog.IndexOf("\v", StringComparison.InvariantCultureIgnoreCase);
                Question = ChatTools.CleanChatLine(rawDialog.Substring(0, num - 1));
                rawDialog.Substring(num + 1).Split(new string[1] { "\a" }, StringSplitOptions.RemoveEmptyEntries).ToList()
                    .ForEach(delegate (string o)
                    {
                        Options.Add(ChatTools.CleanChatLine(o));
                    });
            }
        }

        public class DialogTools
        {
            private readonly IntPtr _instance;

            public int DialogId => GetDialogId(_instance);

            public ushort DialogIndex => GetDialogIndex(_instance);

            public int DialogOptionCount => GetDialogOptionCount(_instance);

            public DialogTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public string GetDialogText()
            {
                byte[] array = new byte[4096];
                if (!GetDialogString(_instance, array, 4096))
                {
                    return string.Empty;
                }

                return Encoding.GetEncoding(1252).GetString(array).Trim(default(char));
            }

            public DialogInfo GetDialog()
            {
                return new DialogInfo(GetDialogText());
            }
        }

        public class EntityTools
        {
            private readonly IntPtr _instance;

            private readonly EliteAPI _api;

            public int LocalPlayerIndex => GetPlayerIndex(_instance);

            public EntityTools(IntPtr apiObject, EliteAPI api)
            {
                _instance = apiObject;
                _api = api;
            }

            public EntityEntry GetStaticEntity(int index)
            {
                byte[] array = new byte[Marshal.SizeOf(typeof(EntityEntry))];
                if (!EliteAPI.GetEntity(_instance, index, array))
                {
                    return new EntityEntry();
                }

                IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
                Marshal.Copy(array, 0, intPtr, array.Length);
                EntityEntry entityEntry = (EntityEntry)Marshal.PtrToStructure(intPtr, typeof(EntityEntry));
                Marshal.FreeHGlobal(intPtr);
                entityEntry.Distance = (float)Math.Sqrt(entityEntry.Distance);
                return entityEntry;
            }

            public XiEntity GetEntity(int index)
            {
                return new XiEntity(_instance, _api, index);
            }

            public int GetEntityPointer(int index)
            {
                return EliteAPI.GetEntityPointer(_instance, index);
            }

            public XiEntity GetLocalPlayer()
            {
                return GetEntity(LocalPlayerIndex);
            }

            public bool GetEntityData(int index, int offset, byte[] buffer, int size)
            {
                return EliteAPI.GetEntityData(_instance, index, offset, buffer, size);
            }

            public bool SetEntityData(int index, int offset, byte[] buffer, int size)
            {
                return EliteAPI.SetEntityData(_instance, index, offset, buffer, size);
            }

            public bool SetEntitySpeed(int index, float speed)
            {
                return SetSpeed(_instance, index, speed);
            }

            public bool SetEntityStatus(int index, uint status)
            {
                return SetStatus(_instance, index, status);
            }

            public bool SetEntityRace(int index, byte raceId)
            {
                return SetRace(_instance, index, raceId);
            }

            public bool SetEntityFace(int index, ushort faceId)
            {
                return SetFace(_instance, index, faceId);
            }

            public bool SetEntityHead(int index, ushort headId)
            {
                return SetHead(_instance, index, headId);
            }

            public bool SetEntityBody(int index, ushort bodyId)
            {
                return SetBody(_instance, index, bodyId);
            }

            public bool SetEntityHands(int index, ushort handsId)
            {
                return SetHands(_instance, index, handsId);
            }

            public bool SetEntityLegs(int index, ushort legsId)
            {
                return SetLegs(_instance, index, legsId);
            }

            public bool SetEntityFeet(int index, ushort feetId)
            {
                return SetFeet(_instance, index, feetId);
            }

            public bool SetEntityMain(int index, ushort mainId)
            {
                return SetMain(_instance, index, mainId);
            }

            public bool SetEntitySub(int index, ushort subId)
            {
                return SetSub(_instance, index, subId);
            }

            public bool SetEntityRanged(int index, ushort rangedId)
            {
                return SetRanged(_instance, index, rangedId);
            }

            public bool SetEntityModelSize(int index, float size)
            {
                return SetModelSize(_instance, index, size);
            }

            public bool SetEntityCostumeId(int index, ushort costumeId)
            {
                return SetCostumeId(_instance, index, costumeId);
            }

            public bool SetEntityRenderFlag00(int index, uint flag)
            {
                return SetRenderFlag00(_instance, index, flag);
            }

            public bool SetEntityRenderFlag01(int index, uint flag)
            {
                return SetRenderFlag01(_instance, index, flag);
            }

            public bool SetEntityRenderFlag02(int index, uint flag)
            {
                return SetRenderFlag02(_instance, index, flag);
            }

            public bool SetEntityRenderFlag03(int index, uint flag)
            {
                return SetRenderFlag03(_instance, index, flag);
            }

            public bool SetEntityRenderFlag04(int index, uint flag)
            {
                return SetRenderFlag04(_instance, index, flag);
            }

            public bool SetEntityActionTimer1(int index, ushort timer)
            {
                return SetActionTimer1(_instance, index, timer);
            }

            public bool SetEntityActionTimer2(int index, ushort timer)
            {
                return SetActionTimer2(_instance, index, timer);
            }

            public bool SetEntityFishingTimer(int index, ushort timer)
            {
                return SetFishingTimer(_instance, index, timer);
            }

            public bool SetEntityXPosition(int index, float pos)
            {
                return SetXPosition(_instance, index, pos);
            }

            public bool SetEntityYPosition(int index, float pos)
            {
                return SetYPosition(_instance, index, pos);
            }

            public bool SetEntityZPosition(int index, float pos)
            {
                return SetZPosition(_instance, index, pos);
            }

            public bool SetEntityHPosition(int index, float pos)
            {
                return SetHPosition(_instance, index, pos);
            }
        }

        public class FishTools
        {
            private readonly IntPtr _instance;

            public bool HasFishOnLine => HasFishOnLine(_instance);

            public int Stamina
            {
                get
                {
                    return GetFishStamina(_instance);
                }
                set
                {
                    SetFishStamina(_instance, value);
                }
            }

            public int MaxStamina => GetFishMaxStamina(_instance);

            public short FightTime
            {
                get
                {
                    return GetFishFightTime(_instance);
                }
                set
                {
                    SetFishFightTime(_instance, value);
                }
            }

            public int Id1 => GetFishId1(_instance);

            public int Id2 => GetFishId2(_instance);

            public int Id3 => GetFishId3(_instance);

            public int Id4 => GetFishId4(_instance);

            public FishTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public void FightFish()
            {
                EliteAPI.FightFish(_instance);
            }
        }

        public class InventoryTools
        {
            private readonly IntPtr _instance;

            public string SelectedItemName
            {
                get
                {
                    StringBuilder stringBuilder = new StringBuilder(1024);
                    uint selectedItemName = GetSelectedItemName(_instance, stringBuilder, 1024);
                    return stringBuilder.ToString(0, (int)selectedItemName);
                }
            }

            public uint SelectedItemId => GetSelectedItemId(_instance);

            public uint SelectedItemIndex => GetSelectedItemIndex(_instance);

            public uint ShopItemCount => GetShopItemCount(_instance);

            public uint ShopItemCountMax
            {
                get
                {
                    return GetShopItemCountMax(_instance);
                }
                set
                {
                    SetShopItemCount(_instance, (byte)value);
                }
            }

            public InventoryTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public int GetContainerCount(int containerId)
            {
                return EliteAPI.GetContainerCount(_instance, containerId);
            }

            public int GetContainerMaxCount(int containerId)
            {
                return EliteAPI.GetContainerMaxCount(_instance, containerId);
            }

            public InventoryItem GetContainerItem(int containerId, int itemIndex)
            {
                byte[] array = new byte[Marshal.SizeOf(typeof(InventoryItem))];
                EliteAPI.GetContainerItem(_instance, containerId, itemIndex, array);
                IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
                Marshal.Copy(array, 0, intPtr, array.Length);
                InventoryItem result = (InventoryItem)Marshal.PtrToStructure(intPtr, typeof(InventoryItem));
                Marshal.FreeHGlobal(intPtr);
                return result;
            }

            public InventoryItem GetEquippedItem(int slotId)
            {
                byte[] array = new byte[Marshal.SizeOf(typeof(InventoryItem))];
                EliteAPI.GetEquippedItem(_instance, slotId, array);
                IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
                Marshal.Copy(array, 0, intPtr, array.Length);
                InventoryItem result = (InventoryItem)Marshal.PtrToStructure(intPtr, typeof(InventoryItem));
                Marshal.FreeHGlobal(intPtr);
                return result;
            }

            public bool SetBazaarPrice(int price)
            {
                return EliteAPI.SetBazaarPrice(_instance, price);
            }
        }

        public class MenuTools
        {
            private readonly IntPtr _instance;

            public bool IsMenuOpen => IsMenuOpen(_instance);

            public int MenuItemCount => GetMenuItemsCount(_instance);

            public int MenuIndex
            {
                get
                {
                    return GetMenuIndex(_instance);
                }
                set
                {
                    SetMenuIndex(_instance, value);
                }
            }

            public string MenuName
            {
                get
                {
                    byte[] array = new byte[2048];
                    if (GetMenuName(_instance, array, 2048))
                    {
                        return Encoding.GetEncoding(1252).GetString(array).Trim(default(char));
                    }

                    return string.Empty;
                }
            }

            public string HelpName
            {
                get
                {
                    byte[] array = new byte[2048];
                    if (GetMenuHelpName(_instance, array, 2048))
                    {
                        return Encoding.GetEncoding(1252).GetString(array).Trim(default(char));
                    }

                    return string.Empty;
                }
            }

            public string HelpDescription
            {
                get
                {
                    byte[] array = new byte[2048];
                    if (GetMenuHelpString(_instance, array, 2048))
                    {
                        return Encoding.GetEncoding(1252).GetString(array).Trim(default(char));
                    }

                    return string.Empty;
                }
            }

            public MenuTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }
        }

        public class PartyTools
        {
            private readonly IntPtr _instance;

            public PartyTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public AllianceInfo GetAllianceInfo()
            {
                byte[] array = new byte[Marshal.SizeOf(typeof(AllianceInfo))];
                if (!EliteAPI.GetAllianceInfo(_instance, array))
                {
                    return new AllianceInfo();
                }

                IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
                Marshal.Copy(array, 0, intPtr, array.Length);
                AllianceInfo result = (AllianceInfo)Marshal.PtrToStructure(intPtr, typeof(AllianceInfo));
                Marshal.FreeHGlobal(intPtr);
                return result;
            }

            public List<PartyMember> GetPartyMembers()
            {
                byte[] array = new byte[Marshal.SizeOf(typeof(PartyMember)) * 18];
                if (!EliteAPI.GetPartyMembers(_instance, array))
                {
                    return new List<PartyMember>();
                }

                IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
                Marshal.Copy(array, 0, intPtr, array.Length);
                List<PartyMember> list = new List<PartyMember>();
                for (int i = 0; i < 18; i++)
                {
                    PartyMember item = (PartyMember)Marshal.PtrToStructure(new IntPtr(intPtr.ToInt32() + i * Marshal.SizeOf(typeof(PartyMember))), typeof(PartyMember));
                    list.Add(item);
                }

                Marshal.FreeHGlobal(intPtr);
                return list;
            }

            public PartyMember GetPartyMember(int index)
            {
                byte[] array = new byte[Marshal.SizeOf(typeof(PartyMember)) * 18];
                if (!EliteAPI.GetPartyMembers(_instance, array))
                {
                    return new PartyMember();
                }

                IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
                Marshal.Copy(array, 0, intPtr, array.Length);
                PartyMember result = (PartyMember)Marshal.PtrToStructure(new IntPtr(intPtr.ToInt32() + index * Marshal.SizeOf(typeof(PartyMember))), typeof(PartyMember));
                Marshal.FreeHGlobal(intPtr);
                return result;
            }
        }

        public class PlayerTools
        {
            private readonly IntPtr _instance;

            private readonly EliteAPI _api;

            public string Name => _api.Entity.GetLocalPlayer().Name;

            public uint TargetID => _api.Entity.GetLocalPlayer().TargetID;

            public uint ServerID => _api.Entity.GetLocalPlayer().ServerID;

            public ushort PetIndex => _api.Entity.GetLocalPlayer().PetIndex;

            public XiEntity Pet => _api.Entity.GetEntity(PetIndex);

            public float X
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().X;
                }
                set
                {
                    _api.Entity.SetEntityXPosition(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public float Y
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Y;
                }
                set
                {
                    _api.Entity.SetEntityYPosition(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public float Z
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Z;
                }
                set
                {
                    _api.Entity.SetEntityZPosition(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public float H
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().H;
                }
                set
                {
                    _api.Entity.SetEntityHPosition(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public float Speed
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Speed;
                }
                set
                {
                    _api.Entity.SetEntitySpeed(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public uint Status
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Status;
                }
                set
                {
                    _api.Entity.SetEntityStatus(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public byte Type => _api.Entity.GetLocalPlayer().Type;

            public int SpawnFlags => _api.Entity.GetLocalPlayer().SpawnFlags;

            public byte Race
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Race;
                }
                set
                {
                    _api.Entity.SetEntityRace(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort Face
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Face;
                }
                set
                {
                    _api.Entity.SetEntityFace(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort Head
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Head;
                }
                set
                {
                    _api.Entity.SetEntityHead(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort Body
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Body;
                }
                set
                {
                    _api.Entity.SetEntityBody(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort Hands
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Hands;
                }
                set
                {
                    _api.Entity.SetEntityHands(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort Legs
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Legs;
                }
                set
                {
                    _api.Entity.SetEntityLegs(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort Feet
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Feet;
                }
                set
                {
                    _api.Entity.SetEntityFeet(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort Main
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Main;
                }
                set
                {
                    _api.Entity.SetEntityMain(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort Sub
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Sub;
                }
                set
                {
                    _api.Entity.SetEntitySub(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort Ranged
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Ranged;
                }
                set
                {
                    _api.Entity.SetEntityRanged(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public float ModelSize
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().ModelSize;
                }
                set
                {
                    _api.Entity.SetEntityModelSize(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public ushort CostumeID
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().CostumeID;
                }
                set
                {
                    _api.Entity.SetEntityCostumeId(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public uint Render0000
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Render0000;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag00(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public uint Render0001
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Render0001;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag01(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public uint Render0002
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Render0002;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag02(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public uint Render0003
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Render0003;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag03(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public uint Render0004
            {
                get
                {
                    return _api.Entity.GetLocalPlayer().Render0004;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag04(_api.Entity.LocalPlayerIndex, value);
                }
            }

            public uint HP => _api.Party.GetPartyMember(0).CurrentHP;

            public uint MP => _api.Party.GetPartyMember(0).CurrentMP;

            public uint HPP => _api.Party.GetPartyMember(0).CurrentHPP;

            public uint MPP => _api.Party.GetPartyMember(0).CurrentMPP;

            public uint TP => _api.Party.GetPartyMember(0).CurrentTP;

            public uint HPMax => GetPlayerInfo().HPMax;

            public uint MPMax => GetPlayerInfo().MPMax;

            public byte MainJob => GetPlayerInfo().MainJob;

            public byte MainJobLevel => GetPlayerInfo().MainJobLevel;

            public byte SubJob => GetPlayerInfo().SubJob;

            public byte SubJobLevel => GetPlayerInfo().SubJobLevel;

            public ushort ExpCurrent => GetPlayerInfo().ExpCurrent;

            public ushort ExpNeeded => GetPlayerInfo().ExpNeeded;

            public PlayerStats Stats => GetPlayerInfo().Stats;

            public PlayerStats StatsModifiers => GetPlayerInfo().StatsModifiers;

            public short Attack => GetPlayerInfo().Attack;

            public short Defense => GetPlayerInfo().Defense;

            public PlayerElements Elements => GetPlayerInfo().Elements;

            public ushort Title => GetPlayerInfo().Title;

            public ushort Rank => GetPlayerInfo().Rank;

            public ushort RankPoints => GetPlayerInfo().RankPoints;

            public byte Nation => GetPlayerInfo().Nation;

            public byte Residence => GetPlayerInfo().Residence;

            public uint Homepoint => GetPlayerInfo().Homepoint;

            public PlayerCombatSkills CombatSkills => GetPlayerInfo().CombatSkills;

            public PlayerCraftSkills CraftSkills => GetPlayerInfo().CraftSkills;

            public ushort LimitPoints => GetPlayerInfo().LimitPoints;

            public byte MeritPoints => GetPlayerInfo().MeritPoints;

            public byte LimitMode => GetPlayerInfo().LimitMode;

            public short[] Buffs => GetPlayerInfo().Buffs;

            public int LoginStatus => GetPlayerLoginStatus(_instance);

            public int ServerId => GetPlayerServerId(_instance);

            public int ViewMode
            {
                get
                {
                    return GetPlayerViewMode(_instance);
                }
                set
                {
                    SetPlayerViewMode(_instance, value);
                }
            }

            public int ZoneId => GetPlayerZone(_instance);

            public uint PetTP => GetPetTP(_instance);

            public PlayerTools(IntPtr apiObject, EliteAPI api)
            {
                _instance = apiObject;
                _api = api;
            }

            public PlayerInfo GetPlayerInfo()
            {
                byte[] array = new byte[Marshal.SizeOf(typeof(PlayerInfo))];
                EliteAPI.GetPlayerInfo(_instance, array);
                IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
                Marshal.Copy(array, 0, intPtr, array.Length);
                PlayerInfo result = (PlayerInfo)Marshal.PtrToStructure(intPtr, typeof(PlayerInfo));
                Marshal.FreeHGlobal(intPtr);
                return result;
            }

            public PlayerJobPoints GetJobPoints(int jobId)
            {
                try
                {
                    byte[] array = new byte[Marshal.SizeOf(typeof(PlayerJobPoints))];
                    EliteAPI.GetJobPoints(_instance, jobId, array);
                    IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
                    Marshal.Copy(array, 0, intPtr, array.Length);
                    PlayerJobPoints result = (PlayerJobPoints)Marshal.PtrToStructure(intPtr, typeof(PlayerJobPoints));
                    Marshal.FreeHGlobal(intPtr);
                    return result;
                }
                catch
                {
                    return null;
                }
            }

            public byte[] GetSetBlueMagicSpells()
            {
                byte[] array = new byte[20];
                EliteAPI.GetSetBlueMagicSpells(_instance, array);
                return array;
            }

            public bool HasBlueMagicSpellSet(int spellId)
            {
                byte[] setBlueMagicSpells = GetSetBlueMagicSpells();
                int spell = spellId - 512;
                return setBlueMagicSpells.Any((byte s) => s == spell);
            }

            public bool HasKeyItem(uint id)
            {
                return EliteAPI.HasKeyItem(_instance, id);
            }

            public bool HasAbility(uint id)
            {
                return EliteAPI.HasAbility(_instance, id);
            }

            public bool HasSpell(uint id)
            {
                return EliteAPI.HasSpell(_instance, id);
            }

            public bool HasTrait(uint id)
            {
                return EliteAPI.HasTrait(_instance, id);
            }

            public bool HasPetCommand(uint id)
            {
                return EliteAPI.HasPetCommand(_instance, id);
            }

            public bool HasWeaponSkill(uint id)
            {
                return EliteAPI.HasWeaponSkill(_instance, id);
            }
        }

        public class RecastTools
        {
            private readonly IntPtr _instance;

            public RecastTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public int GetAbilityId(int index)
            {
                return EliteAPI.GetAbilityId(_instance, index);
            }

            public List<int> GetAbilityIds()
            {
                List<int> list = new List<int> { 0 };
                for (int i = 0; i < 32; i++)
                {
                    int abilityId = EliteAPI.GetAbilityId(_instance, i);
                    if (abilityId != 0)
                    {
                        list.Add(abilityId);
                    }
                }

                return list;
            }

            public int GetAbilityRecast(int index)
            {
                return EliteAPI.GetAbilityRecast(_instance, index);
            }

            public int GetSpellRecast(int index)
            {
                return EliteAPI.GetSpellRecast(_instance, index);
            }
        }

        public class ResourceTools
        {
            private readonly IntPtr _instance;

            public ResourceTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public IAbility GetAbility(uint id)
            {
                return (IAbility)Marshal.PtrToStructure(GetAbilityById(_instance, id), typeof(IAbility));
            }

            public IAbility GetAbility(string name, int languageId)
            {
                return (IAbility)Marshal.PtrToStructure(GetAbilityByName(_instance, name, languageId), typeof(IAbility));
            }

            public IAbility GetAbilityByTimerId(uint id)
            {
                return (IAbility)Marshal.PtrToStructure(EliteAPI.GetAbilityByTimerId(_instance, id), typeof(IAbility));
            }

            public ISpell GetSpell(uint id)
            {
                return (ISpell)Marshal.PtrToStructure(GetSpellById(_instance, id), typeof(ISpell));
            }

            public ISpell GetSpell(string name, int languageId)
            {
                return (ISpell)Marshal.PtrToStructure(GetSpellByName(_instance, name, languageId), typeof(ISpell));
            }

            public IItem GetItem(uint id)
            {
                return (IItem)Marshal.PtrToStructure(GetItemById(_instance, id), typeof(IItem));
            }

            public IItem GetItem(string name, int languageId)
            {
                return (IItem)Marshal.PtrToStructure(GetItemByName(_instance, name, languageId), typeof(IItem));
            }

            public string GetString(string table, uint index, int languageId)
            {
                return Marshal.PtrToStringAnsi(EliteAPI.GetString(_instance, table, index, languageId));
            }

            public uint GetString(string table, string str, int languageId)
            {
                return GetStringIndex(_instance, table, str, languageId);
            }
        }

        public class XiEntity
        {
            private readonly IntPtr _instance;

            private readonly EliteAPI _api;

            private readonly int _index;

            public uint TargetID => _api.Entity.GetStaticEntity(_index).TargetID;

            public uint ServerID => _api.Entity.GetStaticEntity(_index).ServerID;

            public uint ClaimID => _api.Entity.GetStaticEntity(_index).ClaimID;

            public ushort TargetingIndex => _api.Entity.GetStaticEntity(_index).TargetingIndex;

            public string Name => _api.Entity.GetStaticEntity(_index).Name;

            public byte HealthPercent => _api.Entity.GetStaticEntity(_index).HealthPercent;

            public byte ManaPercent => _api.Entity.GetStaticEntity(_index).ManaPercent;

            public float X
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).X;
                }
                set
                {
                    _api.Entity.SetEntityXPosition(_index, value);
                }
            }

            public float Y
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Y;
                }
                set
                {
                    _api.Entity.SetEntityYPosition(_index, value);
                }
            }

            public float Z
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Z;
                }
                set
                {
                    _api.Entity.SetEntityZPosition(_index, value);
                }
            }

            public float H
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).H;
                }
                set
                {
                    _api.Entity.SetEntityHPosition(_index, value);
                }
            }

            public float Distance => _api.Entity.GetStaticEntity(_index).Distance;

            public float Speed
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Speed;
                }
                set
                {
                    _api.Entity.SetEntitySpeed(_index, value);
                }
            }

            public float SpeedAnimation => _api.Entity.GetStaticEntity(_index).SpeedAnimation;

            public uint Status
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Status;
                }
                set
                {
                    _api.Entity.SetEntityStatus(_index, value);
                }
            }

            public uint WarpPointer => _api.Entity.GetStaticEntity(_index).WarpPointer;

            public uint PetOwnerID => _api.Entity.GetStaticEntity(_index).PetOwnerID;

            public ushort PetIndex => _api.Entity.GetStaticEntity(_index).PetIndex;

            public byte Type => _api.Entity.GetStaticEntity(_index).Type;

            public int SpawnFlags => _api.Entity.GetStaticEntity(_index).SpawnFlags;

            public byte Race
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Race;
                }
                set
                {
                    _api.Entity.SetEntityRace(_index, value);
                }
            }

            public ushort Face
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Face;
                }
                set
                {
                    _api.Entity.SetEntityFace(_index, value);
                }
            }

            public ushort Head
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Head;
                }
                set
                {
                    _api.Entity.SetEntityHead(_index, value);
                }
            }

            public ushort Body
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Body;
                }
                set
                {
                    _api.Entity.SetEntityBody(_index, value);
                }
            }

            public ushort Hands
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Hands;
                }
                set
                {
                    _api.Entity.SetEntityHands(_index, value);
                }
            }

            public ushort Legs
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Legs;
                }
                set
                {
                    _api.Entity.SetEntityLegs(_index, value);
                }
            }

            public ushort Feet
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Feet;
                }
                set
                {
                    _api.Entity.SetEntityFeet(_index, value);
                }
            }

            public ushort Main
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Main;
                }
                set
                {
                    _api.Entity.SetEntityMain(_index, value);
                }
            }

            public ushort Sub
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Sub;
                }
                set
                {
                    _api.Entity.SetEntitySub(_index, value);
                }
            }

            public ushort Ranged
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Ranged;
                }
                set
                {
                    _api.Entity.SetEntityRanged(_index, value);
                }
            }

            public float ModelSize
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).ModelSize;
                }
                set
                {
                    _api.Entity.SetEntityModelSize(_index, value);
                }
            }

            public ushort CostumeID
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).CostumeID;
                }
                set
                {
                    _api.Entity.SetEntityCostumeId(_index, value);
                }
            }

            public uint Render0000
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Render0000;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag00(_index, value);
                }
            }

            public uint Render0001
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Render0001;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag01(_index, value);
                }
            }

            public uint Render0002
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Render0002;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag02(_index, value);
                }
            }

            public uint Render0003
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Render0003;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag03(_index, value);
                }
            }

            public uint Render0004
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).Render0004;
                }
                set
                {
                    _api.Entity.SetEntityRenderFlag04(_index, value);
                }
            }

            public ushort ActionTimer1
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).ActionTimer1;
                }
                set
                {
                    _api.Entity.SetEntityActionTimer1(_index, value);
                }
            }

            public ushort ActionTimer2
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).ActionTimer2;
                }
                set
                {
                    _api.Entity.SetEntityActionTimer2(_index, value);
                }
            }

            public ushort FishingTimer
            {
                get
                {
                    return _api.Entity.GetStaticEntity(_index).FishingTimer;
                }
                set
                {
                    _api.Entity.SetEntityFishingTimer(_index, value);
                }
            }

            public byte[] Animations => _api.Entity.GetStaticEntity(_index).Animations;

            public XiEntity(IntPtr instance, EliteAPI api, int index)
            {
                _api = api;
                _index = index;
                _instance = instance;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public class EntityEntry
        {
            public uint TargetID;

            public uint ServerID;

            public uint ClaimID;

            public ushort TargetingIndex;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 25)]
            public string Name;

            public byte HealthPercent;

            public byte ManaPercent;

            public float X;

            public float Y;

            public float Z;

            public float H;

            public float Distance;

            public float Speed;

            public float SpeedAnimation;

            public uint Status;

            public uint WarpPointer;

            public uint PetOwnerID;

            public ushort PetIndex;

            public byte Type;

            public int SpawnFlags;

            public byte Race;

            public ushort Face;

            public ushort Head;

            public ushort Body;

            public ushort Hands;

            public ushort Legs;

            public ushort Feet;

            public ushort Main;

            public ushort Sub;

            public ushort Ranged;

            public float ModelSize;

            public ushort CostumeID;

            public uint Render0000;

            public uint Render0001;

            public uint Render0002;

            public uint Render0003;

            public uint Render0004;

            public ushort ActionTimer1;

            public ushort ActionTimer2;

            public ushort FishingTimer;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public byte[] Animations;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class InventoryItem
        {
            public ushort Id;

            public ushort Index;

            public uint Count;

            public uint Flag;

            public uint Price;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
            public byte[] Extra;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PlayerStats
        {
            public short Strength;

            public short Dexterity;

            public short Vitality;

            public short Agility;

            public short Intelligence;

            public short Mind;

            public short Charisma;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PlayerElements
        {
            public short Fire;

            public short Ice;

            public short Wind;

            public short Earth;

            public short Lightning;

            public short Water;

            public short Light;

            public short Dark;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PlayerCombatSkill
        {
            public ushort RawSkill;

            public ushort Skill => (ushort)(RawSkill & 0x7FFFu);

            public bool Capped => (RawSkill & 0x8000) != 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PlayerCombatSkills
        {
            public PlayerCombatSkill Unknown;

            public PlayerCombatSkill HandToHand;

            public PlayerCombatSkill Dagger;

            public PlayerCombatSkill Sword;

            public PlayerCombatSkill GreatSword;

            public PlayerCombatSkill Axe;

            public PlayerCombatSkill GreatAxe;

            public PlayerCombatSkill Scythe;

            public PlayerCombatSkill Polearm;

            public PlayerCombatSkill Katana;

            public PlayerCombatSkill GreatKatana;

            public PlayerCombatSkill Club;

            public PlayerCombatSkill Staff;

            public PlayerCombatSkill Unused0000;

            public PlayerCombatSkill Unused0001;

            public PlayerCombatSkill Unused0002;

            public PlayerCombatSkill Unused0003;

            public PlayerCombatSkill Unused0004;

            public PlayerCombatSkill Unused0005;

            public PlayerCombatSkill Unused0006;

            public PlayerCombatSkill Unused0007;

            public PlayerCombatSkill Unused0008;

            public PlayerCombatSkill Unused0009;

            public PlayerCombatSkill Unused0010;

            public PlayerCombatSkill Unused0011;

            public PlayerCombatSkill Archery;

            public PlayerCombatSkill Marksmanship;

            public PlayerCombatSkill Throwing;

            public PlayerCombatSkill Guarding;

            public PlayerCombatSkill Evasion;

            public PlayerCombatSkill Shield;

            public PlayerCombatSkill Parrying;

            public PlayerCombatSkill Divine;

            public PlayerCombatSkill Healing;

            public PlayerCombatSkill Enhancing;

            public PlayerCombatSkill Enfeebling;

            public PlayerCombatSkill Elemental;

            public PlayerCombatSkill Dark;

            public PlayerCombatSkill Summon;

            public PlayerCombatSkill Ninjitsu;

            public PlayerCombatSkill Singing;

            public PlayerCombatSkill String;

            public PlayerCombatSkill Wind;

            public PlayerCombatSkill BlueMagic;

            public PlayerCombatSkill Unused0012;

            public PlayerCombatSkill Unused0013;

            public PlayerCombatSkill Unused0014;

            public PlayerCombatSkill Unused0015;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PlayerCraftSkill
        {
            public ushort RawSkill;

            public bool Capped => (RawSkill & 0x8000) != 0;

            public ushort Rank => (ushort)(RawSkill & 0x1Fu);

            public ushort Skill => (ushort)((RawSkill & 0x1FE0) >> 5);
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PlayerCraftSkills
        {
            public PlayerCraftSkill Fishing;

            public PlayerCraftSkill Woodworking;

            public PlayerCraftSkill Smithing;

            public PlayerCraftSkill Goldsmithing;

            public PlayerCraftSkill Clothcraft;

            public PlayerCraftSkill Leathercraft;

            public PlayerCraftSkill Bonecraft;

            public PlayerCraftSkill Alchemy;

            public PlayerCraftSkill Cooking;

            public PlayerCraftSkill Synergy;

            public PlayerCraftSkill Riding;

            public PlayerCraftSkill Unused0000;

            public PlayerCraftSkill Unused0001;

            public PlayerCraftSkill Unused0002;

            public PlayerCraftSkill Unused0003;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PlayerInfo
        {
            public uint HPMax;

            public uint MPMax;

            public byte MainJob;

            public byte MainJobLevel;

            public byte SubJob;

            public byte SubJobLevel;

            public ushort ExpCurrent;

            public ushort ExpNeeded;

            public PlayerStats Stats;

            public PlayerStats StatsModifiers;

            public short Attack;

            public short Defense;

            public PlayerElements Elements;

            public ushort Title;

            public ushort Rank;

            public ushort RankPoints;

            public byte Nation;

            public byte Residence;

            public uint Homepoint;

            public PlayerCombatSkills CombatSkills;

            public PlayerCraftSkills CraftSkills;

            public ushort LimitPoints;

            public byte MeritPoints;

            public byte LimitMode;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public short[] Buffs;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PlayerJobPoints
        {
            public ushort CapacityPoints;

            public ushort JobPoints;

            public ushort SpentJobPoints;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class AllianceInfo
        {
            public int AllianceLeaderId;

            public int Party0LeaderId;

            public int Party1LeaderId;

            public int Party2LeaderId;

            public byte Party0Visible;

            public byte Party1Visible;

            public byte Party2Visible;

            public byte Party0Count;

            public byte Party1Count;

            public byte Party2Count;

            public byte Invited;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class PartyMember
        {
            public byte Index;

            public byte MemberNumber;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 18)]
            public string Name;

            public uint ID;

            public uint TargetIndex;

            public uint CurrentHP;

            public uint CurrentMP;

            public uint CurrentTP;

            public byte CurrentHPP;

            public byte CurrentMPP;

            public ushort Zone;

            public uint FlagMask;

            public byte MainJob;

            public byte MainJobLvl;

            public byte SubJob;

            public byte SubJobLvl;

            public byte Active;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class TargetInfo
        {
            public uint TargetIndex;

            public uint TargetId;

            public uint TargetEntryPointer;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 25)]
            public string TargetName;

            public byte TargetHealthPercent;

            public uint SubTargetIndex;

            public uint SubTargetId;

            public uint SubTargetEntryPointer;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 25)]
            public string SubTargetName;

            public byte SubTargetHealthPercent;

            [MarshalAs(UnmanagedType.I1)]
            public bool HasSubTarget;

            [MarshalAs(UnmanagedType.I1)]
            public bool LockedOn;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class IAbility
        {
            public ushort ID;

            public byte Type;

            public byte Element;

            public ushort ListIconID;

            public ushort MP;

            public ushort TimerID;

            public ushort ValidTargets;

            public short TP;

            public byte Unknown0000;

            public byte MonsterLevel;

            public char Range;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            public byte[] Unknown0001;

            public byte EOE;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.LPStr)]
            public string[] Name;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.LPStr)]
            public string[] Description;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class ISpell
        {
            public ushort Index;

            public ushort MagicType;

            public ushort Element;

            public ushort ValidTargets;

            public ushort Skill;

            public ushort MPCost;

            public byte CastTime;

            public byte RecastDelay;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public short[] LevelRequired;

            public ushort ID;

            public ushort ListIcon1;

            public ushort ListIcon2;

            public byte Requirements;

            public byte Range;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public byte[] Unknown0000;

            public byte EOE;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.LPStr)]
            public string[] Name;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.LPStr)]
            public string[] Description;
        }

        public struct MonAbility
        {
            public ushort Move;

            public byte Level;

            public byte Unknown0000;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class IItem
        {
            public uint ItemID;

            public ushort Flags;

            public ushort StackSize;

            public ushort ItemType;

            public ushort ResourceID;

            public ushort ValidTargets;

            public ushort Level;

            public ushort Slots;

            public ushort Races;

            public uint Jobs;

            public byte SuperiorLevel;

            public ushort ShieldSize;

            public byte MaxCharges;

            public ushort CastTime;

            public ushort CastDelay;

            public uint RecastDelay;

            public ushort ItemLevel;

            public ushort Damage;

            public ushort Delay;

            public ushort DPS;

            public byte Skill;

            public byte JugSize;

            public ushort InstinctCost;

            public ushort MonipulatorID;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] MonipulatorName;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.Struct)]
            public MonAbility[] MonipulatorAbilities;

            public ushort PuppetSlot;

            public uint PuppetElements;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.LPStr)]
            public string[] Name;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.LPStr)]
            public string[] Description;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.LPStr)]
            public string[] LogNameSingular;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.LPStr)]
            public string[] LogNamePlural;

            public uint ImageSize;

            public byte ImageType;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ImageName;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2424)]
            public byte[] Bitmap;
        }

        public class TargetTools
        {
            private readonly IntPtr _instance;

            public TargetTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public TargetInfo GetTargetInfo()
            {
                byte[] array = new byte[Marshal.SizeOf(typeof(TargetInfo))];
                EliteAPI.GetTargetInfo(_instance, array);
                IntPtr intPtr = Marshal.AllocHGlobal(array.Length);
                Marshal.Copy(array, 0, intPtr, array.Length);
                TargetInfo result = (TargetInfo)Marshal.PtrToStructure(intPtr, typeof(TargetInfo));
                Marshal.FreeHGlobal(intPtr);
                return result;
            }

            public bool SetTarget(int index)
            {
                return EliteAPI.SetTarget(_instance, index);
            }
        }

        public class TradeItem
        {
            public int Index { get; set; }

            public byte ItemIndex { get; set; }

            public ushort ItemId { get; set; }

            public byte ItemCount { get; set; }
        }

        public class TradeMenuTools
        {
            private readonly IntPtr _instance;

            public bool IsTradeMenuOpen => IsTradeMenuOpen(_instance);

            public TradeMenuTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }

            public TradeItem GetTradeItem(int index)
            {
                return new TradeItem
                {
                    Index = index,
                    ItemIndex = GetTradeItemIndex(_instance, index),
                    ItemId = GetTradeItemId(_instance, index),
                    ItemCount = GetTradeItemCount(_instance, index)
                };
            }

            public bool SetTradeItem(int index, TradeItem item)
            {
                return EliteAPI.SetTradeItem(_instance, index, item.ItemId, item.ItemIndex, item.ItemCount);
            }

            public bool SetTradeItems(List<TradeItem> items)
            {
                if (items.Count > 8)
                {
                    return false;
                }

                return !items.Where((TradeItem t, int x) => !SetTradeItem(x, t)).Any();
            }
        }

        public class VanaTimeTools
        {
            private readonly IntPtr _instance;

            public ulong RawTimestamp => GetRawVanaTime(_instance);

            public string CurrentTimestamp
            {
                get
                {
                    StringBuilder stringBuilder = new StringBuilder(1024);
                    uint currentTimestamp = GetCurrentTimestamp(_instance, stringBuilder, 1024);
                    if (currentTimestamp != 0)
                    {
                        return stringBuilder.ToString(0, (int)currentTimestamp);
                    }

                    return string.Empty;
                }
            }

            public uint CurrentHour => GetCurrentHour(_instance);

            public uint CurrentMinute => GetCurrentMinute(_instance);

            public uint CurrentSecond => GetCurrentSecond(_instance);

            public uint CurrentMoonPhase => GetCurrentMoonPhase(_instance);

            public uint CurrentMoonPercent => GetCurrentMoonPercent(_instance);

            public uint CurrentWeekDay => GetCurrentWeekDay(_instance);

            public uint CurrentDay => GetCurrentDay(_instance);

            public uint CurrentMonth => GetCurrentMonth(_instance);

            public uint CurrentYear => GetCurrentYear(_instance);

            public VanaTimeTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }
        }

        public class WeatherTools
        {
            private readonly IntPtr _instance;

            public int CurrentWeather => GetCurrentWeather(_instance);

            public WeatherTools(IntPtr apiObject)
            {
                _instance = apiObject;
            }
        }

        private const string ELITEAPI_LIB = "EliteAPI.dll";

        private IntPtr _instance;

        public AuctionHouseTools AuctionHouse { get; set; }

        public AutoFollowTools AutoFollow { get; set; }

        public CastBarTools CastBar { get; set; }

        public ChatTools Chat { get; set; }

        public CraftMenuTools CraftMenu { get; set; }

        public DialogTools Dialog { get; set; }

        public EntityTools Entity { get; set; }

        public FishTools Fish { get; set; }

        public InventoryTools Inventory { get; set; }

        public MenuTools Menu { get; set; }

        public PartyTools Party { get; set; }

        public PlayerTools Player { get; set; }

        public RecastTools Recast { get; set; }

        public ResourceTools Resources { get; set; }

        public TargetTools Target { get; set; }

        public TradeMenuTools TradeMenu { get; set; }

        public WeatherTools Weather { get; set; }

        public ThirdPartyTools ThirdParty { get; set; }

        public VanaTimeTools VanaTime { get; set; }

        public EliteAPI(int processId)
        {
            _instance = CreateInstance(processId);
            CreateObjects(processId);
        }

        ~EliteAPI()
        {
            try
            {
                if (_instance != IntPtr.Zero)
                {
                    DeleteInstance(_instance);
                }
            }
            catch
            {
            }

            _instance = IntPtr.Zero;
            AuctionHouse = null;
            AutoFollow = null;
            CastBar = null;
            Chat = null;
            CraftMenu = null;
            Dialog = null;
            Entity = null;
            Fish = null;
            Inventory = null;
            Menu = null;
            Party = null;
            Player = null;
            Recast = null;
            Resources = null;
            Target = null;
            TradeMenu = null;
            Weather = null;
            ThirdParty = null;
            VanaTime = null;
        }

        public bool Reinitialize(int processId)
        {
            if (_instance != IntPtr.Zero)
            {
                return Reinitialize(_instance, processId);
            }

            _instance = CreateInstance(processId);
            CreateObjects(processId);
            return true;
        }

        private void CreateObjects(int processId)
        {
            AuctionHouse = new AuctionHouseTools(_instance);
            AutoFollow = new AutoFollowTools(_instance);
            CastBar = new CastBarTools(_instance);
            Chat = new ChatTools(_instance);
            CraftMenu = new CraftMenuTools(_instance);
            Dialog = new DialogTools(_instance);
            Entity = new EntityTools(_instance, this);
            Fish = new FishTools(_instance);
            Inventory = new InventoryTools(_instance);
            Menu = new MenuTools(_instance);
            Party = new PartyTools(_instance);
            Player = new PlayerTools(_instance, this);
            Recast = new RecastTools(_instance);
            Resources = new ResourceTools(_instance);
            Target = new TargetTools(_instance);
            TradeMenu = new TradeMenuTools(_instance);
            Weather = new WeatherTools(_instance);
            ThirdParty = new ThirdPartyTools(_instance);
            VanaTime = new VanaTimeTools(_instance);
        }

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr CreateInstance(int processId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool Reinitialize(IntPtr apiObject, int processId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void DeleteInstance(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetAHItemCountLoaded(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetAHItemCount(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool IsAHDoneLoadingItems(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetAHItemIds(IntPtr apiObject, byte[] lpBuffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool IsAutoFollowing(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetAutoFollowTargetIndex(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetAutoFollowTargetId(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetAutoFollowFollowIndex(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetAutoFollowFollowId(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetAutoFollowInfo(IntPtr apiObject, uint tIndex, uint tId, uint fIndex, uint fId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetAutoFollowCoords(IntPtr apiObject, float fX, float fY, float fZ);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetAutoFollow(IntPtr apiObject, bool follow);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern float GetCastBarMax(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern float GetCastBarCount(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern float GetCastBarPercent(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetChatLineCount(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetChatLineRaw(IntPtr apiObject, int index, byte[] buffer, int size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool IsCraftMenuOpen(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ushort GetCraftItemId(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern byte GetCraftItemIndex(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern byte GetCraftItemCount(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetCraftItem(IntPtr apiObject, int index, ushort itemId, byte itemIndex, byte itemCount);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool IsCrafting(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetDialogId(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetDialogOptionCount(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ushort GetDialogIndex(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetDialogString(IntPtr apiObject, byte[] lpBuffer, int size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetPlayerIndex(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetEntity(IntPtr apiObject, int index, byte[] lpBuffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetEntityPointer(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetEntityData(IntPtr apiObject, int index, int offset, byte[] lpBuffer, int size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetEntityData(IntPtr apiObject, int index, int offset, byte[] lpBuffer, int size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetSpeed(IntPtr apiObject, int index, float speed);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetStatus(IntPtr apiObject, int index, uint status);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetRace(IntPtr apiObject, int index, byte raceId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetFace(IntPtr apiObject, int index, ushort faceId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetHead(IntPtr apiObject, int index, ushort headId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetBody(IntPtr apiObject, int index, ushort bodyId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetHands(IntPtr apiObject, int index, ushort handsId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetLegs(IntPtr apiObject, int index, ushort legsId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetFeet(IntPtr apiObject, int index, ushort feetId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetMain(IntPtr apiObject, int index, ushort mainId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetSub(IntPtr apiObject, int index, ushort subId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetRanged(IntPtr apiObject, int index, ushort rangedId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetModelSize(IntPtr apiObject, int index, float size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetCostumeId(IntPtr apiObject, int index, ushort costumeId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetRenderFlag00(IntPtr apiObject, int index, uint flag);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetRenderFlag01(IntPtr apiObject, int index, uint flag);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetRenderFlag02(IntPtr apiObject, int index, uint flag);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetRenderFlag03(IntPtr apiObject, int index, uint flag);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetRenderFlag04(IntPtr apiObject, int index, uint flag);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetActionTimer1(IntPtr apiObject, int index, ushort timer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetActionTimer2(IntPtr apiObject, int index, ushort timer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetFishingTimer(IntPtr apiObject, int index, ushort timer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetXPosition(IntPtr apiObject, int index, float pos);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetYPosition(IntPtr apiObject, int index, float pos);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetZPosition(IntPtr apiObject, int index, float pos);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetHPosition(IntPtr apiObject, int index, float pos);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool HasFishOnLine(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetFishStamina(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetFishStamina(IntPtr apiObject, int stamina);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetFishMaxStamina(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern short GetFishFightTime(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetFishFightTime(IntPtr apiObject, short time);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetFishId1(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetFishId2(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetFishId3(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetFishId4(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool FightFish(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetContainerCount(IntPtr apiObject, int containerId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetContainerMaxCount(IntPtr apiObject, int containerId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetContainerItem(IntPtr apiObject, int containerId, int itemIndex, byte[] lpBuffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetEquippedItem(IntPtr apiObject, int slotId, byte[] lpBuffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetBazaarPrice(IntPtr apiObject, int price);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetSelectedItemName(IntPtr apiObject, StringBuilder lpBuffer, int nSize);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetSelectedItemId(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetSelectedItemIndex(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetShopItemCount(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetShopItemCount(IntPtr apiObject, byte value);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetShopItemCountMax(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool IsMenuOpen(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetMenuItemsCount(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetMenuIndex(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetMenuIndex(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetMenuName(IntPtr apiObject, byte[] lpBuffer, int size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetMenuHelpName(IntPtr apiObject, byte[] lpBuffer, int size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetMenuHelpString(IntPtr apiObject, byte[] lpBuffer, int size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetAllianceInfo(IntPtr apiObject, byte[] lpBuffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetPartyMembers(IntPtr apiObject, byte[] lpBuffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetPlayerInfo(IntPtr apiObject, byte[] lpBuffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetPlayerZone(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetPlayerViewMode(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetPlayerViewMode(IntPtr apiObject, int viewMode);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetPlayerServerId(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetPlayerLoginStatus(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool HasKeyItem(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool HasAbility(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool HasSpell(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool HasTrait(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool HasPetCommand(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool HasWeaponSkill(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetJobPoints(IntPtr apiObject, int jobId, byte[] buffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetPetTP(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetSetBlueMagicSpells(IntPtr apiObject, byte[] buffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetAbilityId(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetAbilityRecast(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetSpellRecast(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool GetTargetInfo(IntPtr apiObject, byte[] lpBuffer);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetTarget(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool IsTradeMenuOpen(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ushort GetTradeItemId(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern byte GetTradeItemIndex(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern byte GetTradeItemCount(IntPtr apiObject, int index);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern bool SetTradeItem(IntPtr apiObject, int index, ushort itemId, byte itemIndex, byte itemCount);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int GetCurrentWeather(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetAbilityById(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetAbilityByName(IntPtr apiObject, string name, int languageId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetAbilityByTimerId(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetSpellById(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetSpellByName(IntPtr apiObject, string name, int languageId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetItemById(IntPtr apiObject, uint id);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetItemByName(IntPtr apiObject, string name, int languageId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr GetString(IntPtr apiObject, string table, uint index, int languageId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetStringIndex(IntPtr apiObject, string table, string name, int languageId);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void CreateTextObject(IntPtr apiObject, string name);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void DeleteTextObject(IntPtr apiObject, string name);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetText(IntPtr apiObject, string name, string text);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetVisibility(IntPtr apiObject, string name, bool visible);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetFont(IntPtr apiObject, string name, string font, int height);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetColor(IntPtr apiObject, string name, byte a, byte r, byte g, byte b);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetLocation(IntPtr apiObject, string name, float x, float y);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetBold(IntPtr apiObject, string name, bool bold);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetItalic(IntPtr apiObject, string name, bool italic);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetBGColor(IntPtr apiObject, string name, byte a, byte r, byte g, byte b);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetBGBorderSize(IntPtr apiObject, string name, float size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetBGVisibility(IntPtr apiObject, string name, bool visible);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetRightJustified(IntPtr apiObject, string name, bool rightjustified);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void FlushCommands(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SetKey(IntPtr apiObject, byte key, bool down);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void BlockInput(IntPtr apiObject, bool block);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern void SendString(IntPtr apiObject, string str);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int ConsoleIsNewCommand(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int ConsoleGetArgCount(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern int ConsoleGetArg(IntPtr apiObject, int index, byte[] lpBuffer, int size);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern ulong GetRawVanaTime(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentTimestamp(IntPtr apiObject, StringBuilder lpBuffer, int nSize);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentHour(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentMinute(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentSecond(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentMoonPhase(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentMoonPercent(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentWeekDay(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentDay(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentMonth(IntPtr apiObject);

        [DllImport("EliteAPI.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern uint GetCurrentYear(IntPtr apiObject);
    }
}
