using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class MapModuleContact
{
    /// <summary>
    /// Тип контакта. Аналогичный тип должен быть указан в классе Map
    /// </summary>
    [SerializeField] string _contactType;
    /// <summary>
    /// Типы контактов, с которыми соединение текущего контакта запрещено
    /// </summary>
    [SerializeField] List<string> _notSuitableContactTypes;

    public string ContactType => _contactType;
    public List<string> NotSuitableContactTypes => _notSuitableContactTypes;

    /// <summary>
    /// Проверка на возможность соединения этого контакта с другим
    /// </summary>
    /// <param name="other"></param>
    /// <returns>возвращает true, если соединение возможно и false - в противном случае</returns>
    public bool IsMatchingContacts(MapModuleContact other)
    {
        // контакты можно соединить, если их _contactType не содержатся в списках _notSuitableContactTypes друг друга
        return !other.NotSuitableContactTypes.Contains(ContactType) &&
               !NotSuitableContactTypes.Contains(other.ContactType);
    }
}

public static class ContactDirectionInMap
{
    public static Vector2 Forward => new Vector2(0, 1);
    public static Vector3 Back => new Vector2(0, -1);
    public static Vector3 Right => new Vector2(1, 0);
    public static Vector3 Left => new Vector2(-1, 0);
}
