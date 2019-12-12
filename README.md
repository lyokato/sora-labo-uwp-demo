# Sora Labo UWP Application Demo

## 概要

[Sora Labo](https://sora-labo.shiguredo.jp/)用のUWPデスクトップアプリの実験用デモ

## 利用法

Config.csで正しいアカウント名とシグナリングキーを記入してビルド

### 利用可能な機能

- multi-streamによる動画の送受信(音声は現状NG。下記の問題のため)
- single-streamによる、動画、音声の送信
- single-streamによる、動画の受信(音声は現状NG, 下記の問題のため)
- 動画送信時のスクリーンキャスト

sora-android-sdk-samplesアプリを相手にmultistreamのビデオ送受信を行っている時のスクリーンショット

![soralabodemo](https://user-images.githubusercontent.com/30877/70138644-06baba80-16d4-11ea-9669-ebcb877af93a.png)

### 未着手

- simulcast対応
- spotlight対応
- 多人数同時通信のアプリケーションサンプル

## 既知の問題

- 音の受信の設定を入れると、Answerを作ってSetLocalDescriptionに突っ込むとデッドロックが発生して固まる?
- H264を指定するとSetLocalDescriptionでクラッシュ
- see also: https://github.com/webrtc-uwp/webrtc-uwp-sdk/issues
- 公式issueを見ても、音はちょっとハマるとデッドロックしやすそうではある
