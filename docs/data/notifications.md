# Notifications - ONI Data Map
Game build: U58-717001 | Generated: 2026-03-17

## NotificationType Enum

Determines visual style (color, icon), sound, and sort order. Lower numeric value = higher sort priority.

| Value | Name | Icon | Sound | Color Set |
|-|-|-|-|-|
| 1 | Bad | icon_bad | Warning | NotificationBad |
| 2 | Good | icon_normal | Notification | NotificationNormal |
| 3 | BadMinor | icon_normal | Notification | NotificationNormal |
| 4 | Neutral | icon_normal | Notification | NotificationNormal |
| 5 | Tutorial | icon_warning | Notification | NotificationTutorial |
| 6 | Messages | icon_message | Message | NotificationMessage |
| 7 | DuplicantThreatening | icon_threatening | Warning_DupeThreatening | NotificationBad |
| 8 | Event | icon_event | Message | NotificationEvent |
| 9 | MessageImportant | icon_message_important | Message_Important | NotificationMessageImportant |
| 10 | Custom | (per-prefab) | Notification | (per-prefab) |

Sort order: notifications are sorted by `Type` ascending (Bad=1 first), then by `Idx` (creation order).

## Notification Class

Constructor: `Notification(title, type, tooltip, tooltip_data, expires, delay, custom_click_callback, custom_click_data, click_focus, volume_attenuation, clear_on_click, show_dismiss_button)`

### Fields

| Field | Type | Default | Notes |
|-|-|-|-|
| titleText | string | (ctor) | Supports `{NotifierName}` tag replacement |
| Type | NotificationType | (ctor) | Property |
| Notifier | Notifier | null | Set when added via `Notifier.Add()` |
| clickFocus | Transform | null | Camera target on click; auto-set from Notifier if `AutoClickFocus` |
| Time | float | - | Set to `KTime.Instance.UnscaledGameTime` on add |
| GameTime | float | - | Set to `UnityEngine.Time.time` on add |
| Delay | float | 0 | Seconds before notification becomes visible |
| Idx | int | auto-inc | Per-instance creation counter, used for sort stability |
| ToolTip | Func | null | `Func<List<Notification>, object, string>` |
| tooltipData | object | null | Passed as second arg to ToolTip delegate |
| expires | bool | true | If true, removed after `NotificationScreen.lifetime` seconds |
| playSound | bool | true | Play sound on first display |
| volume_attenuation | bool | true | Attenuate sound based on time since last notification |
| customClickCallback | ClickCallback | null | `delegate void ClickCallback(object data)` |
| customClickData | object | null | Passed to customClickCallback |
| clearOnClick | bool | false | Auto-remove after click |
| showDismissButton | bool | false | Show X dismiss button |
| customNotificationID | string | null | Matches `CustomNotificationPrefabs.ID` for Custom type |

### Key Methods

| Method | Signature | Notes |
|-|-|-|
| IsReady | `bool IsReady()` | True when `Time.time >= GameTime + Delay` |
| Clear | `void Clear()` | Removes from Notifier or NotificationManager |

## Notifier Component

KMonoBehaviour added to GameObjects that emit notifications. Registered in `Components.Notifiers`.

| Field | Type | Default | Notes |
|-|-|-|-|
| DisableNotifications | bool | false | Suppresses all `Add()` calls |
| AutoClickFocus | bool | true | Auto-sets `notification.clickFocus` to this transform |

| Method | Signature | Notes |
|-|-|-|
| Add | `void Add(Notification, string suffix)` | Sets NotifierName from KSelectable, registers with NotificationManager |
| Remove | `void Remove(Notification)` | Clears Notifier ref, calls NotificationManager.RemoveNotification |

## NotificationManager

Singleton (`NotificationManager.Instance`). Manages pending/active notification lifecycle.

| Event | Signature | Notes |
|-|-|-|
| notificationAdded | `Action<Notification>` | Fired when notification becomes ready (delay elapsed) |
| notificationRemoved | `Action<Notification>` | Fired on removal |

Flow: `Notifier.Add()` -> `NotificationManager.AddNotification()` -> pending queue -> `Update()` polls `IsReady()` -> `DoAddNotification()` fires event.

## NotificationScreen

Singleton UI. Manages visual display, grouping, and sounds.

### Display Grouping

Notifications with identical `titleText` are grouped into a single `Entry`. Count shown as suffix: `"Title (N)"`. Click cycles through grouped notifications.

### Sound Behavior

- Sounds defined per-type in `InitNotificationSounds()` (see enum table above)
- Suppressed during first 5 seconds after screen init
- Volume attenuated based on `soundDecayTime` (10s) since last notification of same sound
- Grouped Bad/DuplicantThreatening notifications play `_AddCount` sound variant if available

### Expiration

Notifications with `expires=true` are removed when `UnscaledGameTime - notification.Time > lifetime`.

## MessageNotification Subclass

Wraps a `Message` object for the Messages system (research complete, colony achievements, etc.).

| Property | Notes |
|-|-|
| expires | Always false |
| Type | Set from `message.GetMessageType()` (usually Messages or MessageImportant) |
| showDismissButton | Set from `message.ShowDismissButton()` |
| playSound | Set from `message.PlayNotificationSound()` |
| clickFocus | Always null (Messages use dialog display, not camera focus) |

## Common Patterns

```csharp
// Basic expiring notification
var n = new Notification("Something happened", NotificationType.BadMinor);
gameObject.GetComponent<Notifier>().Add(n);

// Non-expiring with tooltip and click callback
var n = new Notification(
    "Alert: {NotifierName}",
    NotificationType.Bad,
    tooltip: (notifs, data) => $"Affects {notifs.Count} buildings",
    expires: false,
    custom_click_callback: (data) => DoSomething(),
    clear_on_click: true
);
notifier.Add(n);

// Standalone (no Notifier component)
var n = new Notification("Global event", NotificationType.Event);
NotificationManager.Instance.AddNotification(n);
```
