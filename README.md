# [WIP] Sora Labo UWP Application Demo

## DESCRIPTION

[Sora Labo](https://sora-labo.shiguredo.jp/)用のUWPデスクトップアプリの実験用デモ

まだちゃんと動かないところが多いです

## USAGE

Config.csで正しいアカウント名とシグナリングキーを記入してビルド

できること

- multi-streamによる動画の送受信(音声は現状NG。下記の問題のため)
- single-streamによる、動画、音声の送信
- single-streamによる、動画の受信(音声は現状NG, 下記の問題のため)


sora-android-sdk-samplesアプリを相手にmultistreamのビデオ送受信を行っている時のスクリーンショット

![soralabodemo](https://user-images.githubusercontent.com/30877/70138644-06baba80-16d4-11ea-9669-ebcb877af93a.png)

## 既知の問題

- 音の受信の設定を入れると、Answerを作ってSetLocalDescriptionに突っ込むとデッドロックが発生して固まる?
- see also: https://github.com/webrtc-uwp/webrtc-uwp-sdk/issues/191
