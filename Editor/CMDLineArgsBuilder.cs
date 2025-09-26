using System;
using System.Text;
using System.IO;

namespace LM.BuildAutomation.Editor
{
    public static class CMDLineArgsBuilder
    {
        /// <summary>
        /// 템플릿(ArgsTemplate)과 플레이스홀더 치환 인자 문자열 생성
        /// 지원 플레이스홀더: {index}, {width}, {height}, {basePort}, {port}, {nickname}, {exeDir}
        /// </summary>
        /// <param name="settings">빌드 자동화 설정 SO</param>
        /// <param name="instanceIndex">인스턴스 인덱스</param>
        /// <returns>치환 완료된 명령줄 인자 문자열</returns>
        public static string Build(BuildAutomationSettings settings, int instanceIndex)
        {
            string template = settings.ArgsTemplate ?? string.Empty;
            int width = settings.WindowWidth;
            int height = settings.WindowHeight;
            int port = settings.BasePort + instanceIndex;
            string nickname = settings.NicknameBase ?? "Player";
            string exeDir = Path.GetDirectoryName(settings.BuildTargetPath) ?? string.Empty;

            string arg = template;
            arg = arg.Replace("{index}", instanceIndex.ToString());
            arg = arg.Replace("{width}", width.ToString());
            arg = arg.Replace("{height}", height.ToString());
            arg = arg.Replace("{basePort}", settings.BasePort.ToString());
            arg = arg.Replace("{port}", port.ToString());
            arg = arg.Replace("{nickname}", QuoteIfNeeded(nickname));
            arg = arg.Replace("{exeDir}", QuoteIfNeeded(exeDir));

            var ret = arg;
            return ret;
        }

        /// <summary>
        /// 공백/탭/따옴표 포함 값 따옴표 감싸기 및 내부 따옴표 제거 처리
        /// </summary>
        private static string QuoteIfNeeded(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                var ret = value ?? string.Empty;
                return ret;
            }
            bool need = value.Contains(" ") || value.Contains("\t") || value.Contains("\"");
            if (need == false)
            {
                var ret = value;
                return ret;
            }
            string removed = value.Replace("\"", "\\\"");
            var result = "\"" + removed + "\"";
            return result;
        }
    }
}