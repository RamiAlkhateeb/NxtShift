using Nxt.UI;

namespace NxtShift.Web.Services;

/// <summary>
/// The app's own copy in Arabic and English (shared component strings live in Nxt.UI's NxtText).
/// Sentences with values use {0} placeholders; never build them by concatenation.
/// </summary>
public sealed class ShiftText(INxtLocale locale)
{
    public string this[string key] =>
        (Strings.TryGetValue(locale.Language, out var table) && table.TryGetValue(key, out var value)) ? value
        : Strings["en"].TryGetValue(key, out var en) ? en
        : key;

    public string T(string key, params object[] args) => string.Format(this[key], args);

    /// <summary>"8h 30m", "8h"</summary>
    public string Duration(int minutes) =>
        minutes % 60 == 0 ? T("duration_h", minutes / 60) : T("duration_hm", minutes / 60, minutes % 60);

    public string Duration(long ms) => Duration((int)(ms / 60_000));

    /// <summary>"Friday, 6 November" (Gregorian in both languages).</summary>
    public string DayDate(DateTimeOffset local)
    {
        var lang = Days.ContainsKey(locale.Language) ? locale.Language : "en";
        return T("date_format", Days[lang][(int)local.DayOfWeek], local.Day, Months[lang][local.Month - 1]);
    }

    private static readonly Dictionary<string, string[]> Days = new()
    {
        ["ar"] = ["الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"],
        ["en"] = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"],
    };

    private static readonly Dictionary<string, string[]> Months = new()
    {
        ["ar"] = ["يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"],
        ["en"] = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"],
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["ar"] = new()
        {
            ["app_name"] = "نكست شفت",
            ["date_format"] = "{0}، {1} {2}",
            ["duration_hm"] = "{0} س {1} د",
            ["duration_h"] = "{0} ساعات",

            ["welcome_title"] = "لا تنسَ تسجيل خروجك أبدًا.",
            ["welcome_sub"] = "نقرة واحدة عند البدء، وتذكير عند انتهاء {0}.",
            ["welcome_step1"] = "سجّل دخولك هنا عندما تبدأ العمل من المنزل.",
            ["welcome_step2"] = "تابع العدّ التنازلي لنوبتك، ويكمل التطبيق من حيث توقف حتى بعد إغلاقه.",
            ["welcome_step3"] = "تسمع صوتًا قصيرًا مع كل ساعة، وصوتًا آخر عند انتهاء النوبة لتسجّل خروجك في نظام العمل. أبقِ التطبيق مفتوحًا.",
            ["welcome_ios"] = "على iPhone: أضف التطبيق إلى الشاشة الرئيسية أولًا (مشاركة ← إضافة إلى الشاشة الرئيسية) لتصلك الإشعارات.",
            ["allow_notifications"] = "السماح بالإشعارات",
            ["not_now"] = "ليس الآن",
            ["continue"] = "متابعة",

            ["home_question"] = "تعمل من المنزل اليوم؟",
            ["hours_per_shift"] = "ساعات النوبة",
            ["if_now"] = "إذا سجّلت دخولك الآن ({0})",
            ["ends_at"] = "تنتهي {0}",
            ["home_reminder_note"] = "سيصلك تذكير لتسجيل الخروج في نظام العمل.",
            ["home_reminder_off"] = "التذكيرات متوقفة. يمكنك تفعيلها من الإعدادات.",
            ["punch_in"] = "سجّل الدخول وابدأ نوبتي",
            ["started_earlier"] = "بدأت أبكر؟ غيّر وقت البدء",

            ["on_shift"] = "في النوبة",
            ["until_punch_out"] = "حتى تسجيل الخروج",
            ["of_length"] = "من {0}",
            ["over_by"] = "بعد نهاية النوبة",
            ["shift_progress"] = "تقدّم النوبة",
            ["started"] = "البداية",
            ["ends"] = "النهاية",
            ["worked"] = "عملت",
            ["reminder_at"] = "تذكير عند {0}، وتنبيه مسبق عند {1}",
            ["reminder_at_only"] = "تذكير عند {0}",
            ["reminder_snoozed"] = "التذكير التالي عند {0}",
            ["reminder_off"] = "التذكيرات متوقفة.",
            ["end_shift_now"] = "إنهاء النوبة الآن",
            ["fix_start"] = "تعديل وقت البدء",

            ["start_sheet_title"] = "متى بدأت؟",
            ["start_label"] = "وقت البدء",
            ["start_yesterday"] = "هذا يوم أمس.",
            ["start_sheet_ends"] = "تنتهي النوبة {0}",
            ["start_shift"] = "ابدأ النوبة",
            ["save"] = "حفظ",
            ["cancel"] = "إلغاء",
            ["start_fixed"] = "تم تعديل وقت البدء.",

            ["timeup_title"] = "انتهت نوبتك.",
            ["timeup_early_title"] = "إنهاء النوبة.",
            ["timeup_body"] = "سجّل خروجك في نظام العمل الآن لتُحتسب ساعاتك بدقة.",
            ["started_chip"] = "البداية {0}",
            ["done_chip"] = "أنجزت {0}",
            ["punched_out"] = "سجّلت خروجي",
            ["remind_again"] = "ذكّرني بعد 5 دقائق",

            ["done_title"] = "أحسنت.",
            ["done_body"] = "سجّلت خروجك. استمتع بوقتك.",
            ["punched_in_label"] = "وقت الدخول",
            ["punched_out_label"] = "وقت الخروج",
            ["total"] = "المجموع",
            ["back_home"] = "العودة للرئيسية",

            ["notif_headsup_title"] = "بقيت 10 دقائق",
            ["notif_headsup_body"] = "تنتهي نوبتك عند {0}.",
            ["notif_end_title"] = "حان وقت تسجيل الخروج",
            ["notif_end_body"] = "انتهت نوبتك. سجّل خروجك في نظام العمل.",
            ["notif_repeat_title"] = "ألم تسجّل خروجك بعد؟",
            ["notif_repeat_body"] = "سجّل خروجك في نظام العمل، ثم اضغط «سجّلت خروجي».",

            ["shift_title"] = "مدة النوبة",
            ["shift_desc"] = "تُطبَّق على كل نوبة، بما فيها النوبة الحالية.",
            ["steps_of"] = "بخطوات 15 دقيقة",
            ["shorter"] = "أقصر بـ 15 دقيقة",
            ["longer"] = "أطول بـ 15 دقيقة",
            ["reminders_title"] = "التذكيرات",
            ["reminders_desc"] = "تعمل التذكيرات ما دام التطبيق مفتوحًا أو استُخدم مؤخرًا، فأبقِه في الخلفية أثناء نوبتك.",
            ["remind_end"] = "ذكّرني عند انتهاء النوبة",
            ["remind_headsup"] = "تنبيه قبلها بـ 10 دقائق",
            ["remind_repeat"] = "كرّر كل 5 دقائق حتى أؤكد",
            ["sounds_title"] = "الأصوات",
            ["sounds_desc"] = "تُسمع الأصوات ما دام التطبيق مفتوحًا. على iPhone أبقِه ظاهرًا على الشاشة.",
            ["sound_hourly"] = "صوت قصير مع كل ساعة تنجزها",
            ["sound_end"] = "صوت عند انتهاء النوبة",
            ["sound_test"] = "تجربة الأصوات",
            ["next_chime"] = "صوت كل ساعة · التالي عند {0}",
            ["device_title"] = "هذا الجهاز",
            ["device_notifications"] = "الإشعارات",
            ["status_granted"] = "مسموحة",
            ["status_denied"] = "محظورة من إعدادات المتصفح",
            ["status_unsupported"] = "غير متاحة هنا",
            ["status_ios_install"] = "أضف التطبيق للشاشة الرئيسية أولًا",
            ["allow"] = "سماح",
            ["device_installed"] = "مضاف إلى الشاشة الرئيسية",
            ["installed"] = "مثبّت",
            ["not_installed"] = "ليس بعد",
        },
        ["en"] = new()
        {
            ["app_name"] = "NxtShift",
            ["date_format"] = "{0}, {1} {2}",
            ["duration_hm"] = "{0}h {1}m",
            ["duration_h"] = "{0}h",

            ["welcome_title"] = "Never forget to punch out.",
            ["welcome_sub"] = "One tap when you start. A reminder when your {0} are up.",
            ["welcome_step1"] = "Punch in here when you start working from home.",
            ["welcome_step2"] = "Watch your shift count down. The app picks up where it left off, even after you close it.",
            ["welcome_step3"] = "You hear a short sound every hour, and a different one when your shift is over, so you punch out on the work system. Keep the app open.",
            ["welcome_ios"] = "On iPhone, add the app to your Home Screen first (Share, then Add to Home Screen) so notifications can reach you.",
            ["allow_notifications"] = "Allow notifications",
            ["not_now"] = "Not now",
            ["continue"] = "Continue",

            ["home_question"] = "Working from home today?",
            ["hours_per_shift"] = "hours per shift",
            ["if_now"] = "If you punch in now ({0})",
            ["ends_at"] = "ends {0}",
            ["home_reminder_note"] = "You'll get a reminder to punch out on the work system.",
            ["home_reminder_off"] = "Reminders are off. You can turn them on in Settings.",
            ["punch_in"] = "Punch in, start my shift",
            ["started_earlier"] = "Started earlier? Change start time",

            ["on_shift"] = "On shift",
            ["until_punch_out"] = "until you punch out",
            ["of_length"] = "of {0}",
            ["over_by"] = "past the end of your shift",
            ["shift_progress"] = "Shift progress",
            ["started"] = "Started",
            ["ends"] = "Ends",
            ["worked"] = "Worked",
            ["reminder_at"] = "Reminder at {0}, with a heads-up at {1}",
            ["reminder_at_only"] = "Reminder at {0}",
            ["reminder_snoozed"] = "Next reminder at {0}",
            ["reminder_off"] = "Reminders are off.",
            ["end_shift_now"] = "End shift now",
            ["fix_start"] = "Fix start time",

            ["start_sheet_title"] = "When did you start?",
            ["start_label"] = "Start time",
            ["start_yesterday"] = "That's yesterday.",
            ["start_sheet_ends"] = "Shift ends {0}",
            ["start_shift"] = "Start shift",
            ["save"] = "Save",
            ["cancel"] = "Cancel",
            ["start_fixed"] = "Start time updated.",

            ["timeup_title"] = "Shift's over.",
            ["timeup_early_title"] = "Ending your shift.",
            ["timeup_body"] = "Punch out on the work system now so your hours are right.",
            ["started_chip"] = "Started {0}",
            ["done_chip"] = "{0} done",
            ["punched_out"] = "I've punched out",
            ["remind_again"] = "Remind me again in 5 min",

            ["done_title"] = "Nice work.",
            ["done_body"] = "You're punched out. Enjoy your evening.",
            ["punched_in_label"] = "Punched in",
            ["punched_out_label"] = "Punched out",
            ["total"] = "Total",
            ["back_home"] = "Back to home",

            ["notif_headsup_title"] = "10 minutes left",
            ["notif_headsup_body"] = "Your shift ends at {0}.",
            ["notif_end_title"] = "Time to punch out",
            ["notif_end_body"] = "Your shift is over. Punch out on the work system.",
            ["notif_repeat_title"] = "Still punched in?",
            ["notif_repeat_body"] = "Punch out on the work system, then tap \"I've punched out\".",

            ["shift_title"] = "Shift length",
            ["shift_desc"] = "Used for every shift, including the one running now.",
            ["steps_of"] = "steps of 15 min",
            ["shorter"] = "Shorter by 15 minutes",
            ["longer"] = "Longer by 15 minutes",
            ["reminders_title"] = "Reminders",
            ["reminders_desc"] = "Reminders work while the app is open or was used recently, so keep it in the background during your shift.",
            ["remind_end"] = "Remind me when the shift ends",
            ["remind_headsup"] = "Heads-up 10 minutes before",
            ["remind_repeat"] = "Repeat every 5 min until I confirm",
            ["sounds_title"] = "Sounds",
            ["sounds_desc"] = "Sounds play while the app is open. On iPhone, keep it on screen.",
            ["sound_hourly"] = "Short sound for every hour done",
            ["sound_end"] = "Sound when the shift ends",
            ["sound_test"] = "Test sounds",
            ["next_chime"] = "Hourly chime · next at {0}",
            ["device_title"] = "This device",
            ["device_notifications"] = "Notifications",
            ["status_granted"] = "Allowed",
            ["status_denied"] = "Blocked in browser settings",
            ["status_unsupported"] = "Not available here",
            ["status_ios_install"] = "Add to Home Screen first",
            ["allow"] = "Allow",
            ["device_installed"] = "Added to Home Screen",
            ["installed"] = "Installed",
            ["not_installed"] = "Not yet",
        },
    };
}
