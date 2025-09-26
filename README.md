# Build Automation (Unity Editor Tool)

Unity에서 멀티 인스턴스 빌드/실행과 창 배치를 자동화하는 에디터 툴입니다. 테스트 멀티플레이 환경을 신속하게 구성하고, 실행 창을 원하는 위치로 자동 배치합니다.

## Features

- **심플 모드**: 현재 프로젝트 설정으로 빠른 빌드/실행
- **프로필 모드**: Unity Build Profile 기반 빌드/실행
- **멀티 인스턴스 실행**: 지정 개수(N)만큼 자동 실행
- **창 배치(Windows)**: 4분할 기본
- **인자 템플릿(CMD Args)**: 포트/닉네임/해상도 전달 (개발 진행 중)

> 소개 영상: [YouTube 데모](https://www.youtube.com/watch?v=c9WRNfEV2D0)

---

## Installation

Unity Package Manager에서 설치:

- Git URL:

```text
https://github.com/lmspace7/BuildAutomation.git?path=Assets/Scripts/Core/BuildAutomation
```

---

## Prerequisites

- Windows 환경 (창 배치/프로세스 제어는 Windows 전용)

---

## Requirements

- Unity 2021+ 권장
- Windows 전용 (macOS/Linux 보장하지 않음)

---

## Quick Start

### 1) 설정 열기
- `에디터툴 > Build Automation > Setting`

### 2) 기본값 설정
- 빌드 폴더 경로, 실행 파일명, 창 크기, 플레이어 수 등을 지정

### 3) 실행
- 심플 모드: `에디터툴 > Build Automation > Run SimpeMode BuildAndRun`
- 프로필 모드: `에디터툴 > Build Automation > Run ProfileModeBuild`

---

## Menu Reference

| 메뉴 | 기능 |
| --- | --- |
| `에디터툴 > Build Automation > Setting` | 설정 에셋 포커스 |
| `에디터툴 > Build Automation > Run SimpeMode BuildAndRun` | 심플 모드 빌드/실행 |
| `에디터툴 > Build Automation > Run ProfileModeBuild` | 프로필 모드 빌드/실행 |

---

## Configuration

설정 에셋: `Assets/Settings/BuildAutomation/BuildAutomationSettings.asset`

```text
필드 요약
- BuildTargetPath: 빌드 산출물 폴더
- ExeName: 실행 파일명(.exe 제외)
- WindowWidth / WindowHeight: 실행 창 크기
- PlayerCount: 실행할 인스턴스 수 (1~4)
- BasePort / NicknameBase / ArgsTemplate: 인자 템플릿 (개발 진행 중)
```

---

## Path Rules

- BuildTargetPath는 **절대/상대** 모두 가능
- 상대 경로는 **프로젝트 루트 기준**으로 내부에서 절대 경로로 정규화
- 산출 exe 경로 예: `<BuildTargetPath>/<ExeName>.exe`

---

## Execution Results

- 빌드 성공 시:
  - 기존 동일 exe 프로세스 정리(중복/충돌 방지)
  - 지정 개수만큼 자동 실행
  - Windows에서 창 위치 자동 배치(4분할 또는 사용자 좌표)

---

## Notes

- 이 패키지는 **Windows에서만 실행을 지원**합니다. macOS/Linux에서는 동작을 보장하지 않습니다.
- CMD 인자 기능은 **개발 진행 중**입니다.

---

## License

MIT License — 자세한 내용은 `LICENSE`를 참고하세요.
