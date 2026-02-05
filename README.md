# 2DSpeaker
ゆっくり解説のような動画を簡単に作るためのUnityプロジェクト

## 動作イメージ
StreamingAssets以下に必要なファイルを置いて実行する。音声ファイルや画像ファイルなどを直接読み込むので、実行ファイル作成後も各ファイルを差し替えることで、再ビルドなしで違う動画を作れる。
[この動画](https://www.nicovideo.jp/watch/sm38038352)を作成する際に使用していたものを、修正したもの。
ただし、動画ファイルを作成する機能はないので、実行ファイルを動かして、そのウィンドウをキャプチャするような形になる。

## 必要ファイル

* 初期化データ
初期BGMやテキストボックスの設定など、初期設定に必要な情報が含まれているデータ。
Assets/StreamingAssets/Script/test/playInitializeData.json として配置する。jsonはPlaySceneInitializeDataをシリアライズしたもの。

* 実行スクリプト
音声再生やキャラ表示など、実際に動かす命令群が含まれているスクリプト。
Assets/StreamingAssets/Script/test/execlines.json として配置する。JsonCommandScriptをシリアライズしたもの。
JsonCommandDataがどのようにあるべきかについては、Assets/Resource/CommandSpecs/*.assetに記述されている。これらはCommandSpec のスクリプタブルオブジェクトで、CommandSpec.m_commandNameがJsonCommandData.nameに、CommandSpec.m_paramInfos.paramNameがJsonCommandData.paramArray.keyに対応していて、JsonCommandData.paramArray.valueはCommandSpec.m_paramInfos.typeNameの型にキャスト出来る必要がある。ただし、typeNameの中には型になっていないものがあり、それらについてはstringにキャストしようとする。

* キャラクターデータ
表示するキャラクターの感情(変更画像セット)データの位置や口パクやまばたきにまつわるデータ、テキストの色など、キャラクターに関連するデータ。
Assets\StreamingAssets\Character\characterData.json として配置する。CharacterData をシリアライズしたもの。

* 感情データ
キャラクターの感情(変更画像セット)を設定するデータ。このデータに合わせて、体・顔・目・口・眉・後・他の7箇所の画像を一度に変更したりしなかったりする。
キャラクターデータ中のemotionListPathが指すファイルパスに配置する。EmotionData をシリアライズしたもの。

* エンディングデータ
Assets/StreamingAssets/Script/test/endingData.jsonとして配置する。EndingDataをシリアライズしたもの。クレジット表示に使うもので、上記ファイルたちとは独立して使用されるので、なくてもいいかもしれない。

* その他アセット
初期化データや感情データ、実行スクリプトやエンディングデータに含まれるファイルは、すべてStreaming上の特定フォルダに配置する。BGMはStreamingAssets/BGM、感情データ画像はStreamingAssets/Character、感情以外の画像はStreamingAssets/Image、音声はStreamingAssets/Voice 以下に配置する。サブフォルダに配置することに問題はないが、対応するデータは特定フォルダからの相対パスで記述する必要がある。

また、フォント変更についてはOSにインストールしたものだけが使用出来る。

