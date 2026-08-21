# Changelog

## 1.3.0

### 日本語

- 陣営の全滅で勝敗が決まったとき、決め手となったプレイヤーを発表する演出を挟むようにしました
- ロビー設定「会議中の敵リスポーン係数」を追加しました。会議中に消えた敵の復活と復活間隔の短縮がどれだけ進むかを0〜100%で決められます。0%なら会議の前後で敵の状況が変わらず、100%は従来どおりです
- 会議開催カウントダウン中にも貴重品詰みによる人狼の勝利が成立するようにしました
- 残り5分のチャイムと同時に警告演出が流れるようにしました
- 議論開始時に演出を追加しました
- 会議開始時に出ていたビーコン使用回数の通知をやめ、会議冒頭のTaxManの要約だけで伝えるようにしました
- 会議の投票パネルに残り時間バーを追加しました。左端の赤い部分は投票では削れない最後の10秒です
- 会議チャットログを同じ試合の間は消さずに持ち越すようにしました。過去の会議の発言もスクロールで遡って読め、会議の区切りは見出しとスクロールバーの目盛りで分かります。ヘッダー右端の▲▼ボタンで前後の会議へジャンプできます
- 会議チャットログ・感想戦チャットに話者フィルタを追加しました。発言者の名前・アバターをクリックするとその人の発言だけが表示され、タイトルの「フィルター中」をクリックすると解除できます
- 開票演出をリニューアルしました。得票数もより見やすくなっています
- 会議中のチュートリアルを画面左下のTaxManの吹き出しで表示するようにしました

### English

- When a match is decided by a team being wiped out, the game now shows an announcement of the player who decided it
- Added the lobby setting "Enemy respawn clock during meetings". It sets how far the respawn of downed enemies and the shortening of the respawn interval progress during a meeting, anywhere from 0 to 100%. At 0% enemies come back exactly as they were before the meeting; 100% works as before
- A werewolf win by valuables checkmate can now be established during the meeting countdown
- The five-minute chime is now accompanied by a warning banner sweeping across the screen
- Added an effect when discussion begins
- Removed the beacon usage notification shown at the start of a meeting; the count is now only in TaxMan's recap at the top of the meeting
- The meeting vote panel now shows a remaining-time bar. The red part at the left end is the final 10 seconds that votes cannot cut into
- The meeting chat log is now kept for the whole match instead of being cleared every meeting, so you can scroll back to what was said in earlier meetings. Headings and gold marks on the scrollbar show where each meeting begins, and the ▲▼ buttons at the right of the header jump between meetings
- Added a speaker filter to the meeting chat log and post-match chat. Click a speaker's name or avatar to show only that player's messages, and click the "Filtering" title to clear it
- Reworked the vote reveal. Vote counts are easier to read, too
- Tutorial tips during meetings are now shown in a speech bubble from TaxMan at the bottom left of the screen

## 1.2.0

### 日本語

- 試合結果画面に「リプレイ再生」を追加しました。ゲーム内マップを背景にプレイヤー・敵・アイテム・死体・貴重品の損失や納品・抽出ポイントの状態を時系列で見返せます。再生・速度・シーク・軌跡追跡に対応し、会議中の発言と開票結果も再現します。リプレイはファイルへ保存でき、MODに同梱した日英対応の外部ビューアで後から再生できます
- 試合結果画面に、死亡していたプレイヤーも参加できる「感想戦チャット」と、ホスト用の「ロビーへ戻る」ボタンを追加しました。結果画面では敵を退場させ、無効試合の確認画面がチャットより手前に表示されるようにしました。自動帰還の既定時間は60秒から120秒、設定上限は300秒へ延長しました
- 会議チャットログを閉じている間の新着を赤い未読マークで知らせるようにしました。また、会議招集からTaxManの冒頭投稿までの発言が会議ログやリプレイへ残らないよう、記録・通知の開始を議論開始時に揃えました
- 死体通報ができない間（会議開催のカウントダウン中・最終納品の直前）、右下の通報アイコンを非表示にする代わりに、バツ印とTaxManの顔を重ねた表示へ切り替えるようにしました（アイコンが消えて「通報はどこ？」と迷わないため）
- 通信による位置のズレで、爆弾魔が近くに居続けても「爆弾を仕掛けられたかもしれない…」の警告が出ないまま爆弾を仕掛けられることがある問題を修正しました
- 会議後の分散スポーンの着地点から斜面を除外しました（転倒したままワープすると斜面で転がり続け、操作できないまま転落死する恐れがあるため）
- 人狼陣営の「貴重品を記録する」をラウンド開始時はONにしました

### English

- Added a match replay to the result screen. It shows players, enemies, items, bodies, valuable losses and deliveries, and extraction point states over the game map. Playback, speed, seek, and trail controls are included, along with meeting chat and vote results. Replays can be saved and watched later in the bundled English/Japanese browser viewer
- Added post-match chat—including messages from players who died—and a host-only "Return to Lobby" button to the result screen. Enemies are removed while the result screen is open, and the no-contest confirmation now stays in front of the chat panel. The default automatic return time has increased from 60 to 120 seconds, with a new maximum of 300 seconds
- The meeting chat log now marks unread messages with a red dot while closed. Chat logging, notifications, and replay recording now begin when discussion starts after TaxMan's opening post, so messages from the meeting countdown and intro are not kept
- While reporting a body is unavailable (during the meeting countdown and right before the final delivery), the report icon in the bottom right now shows a cross mark with TaxMan's face over it instead of disappearing, so you can tell reporting is blocked rather than missing
- Fixed the "Someone may have planted a bomb on me…" warning sometimes not appearing before a bomb was planted, because of network position drift between players
- Post-meeting scatter spawns no longer land on slopes (a player warped while tumbling could keep rolling down the slope and fall to their death, unable to recover)
- Valuable recording for the werewolf team now starts enabled at the beginning of each round

## 1.1.0

### 日本語

- 無効試合を追加しました。バグ・切断・事故で続行できなくなったとき、ホストが`VoidMatchKey`（既定F5）の長押しで試合を打ち切れます（確認画面のあと、もう一度長押しで確定）。勝者はつかず、コスメティックトークンも配布されませんが、結果画面と試合の記録は通常どおり表示されます
- 会議の後、生存者が多い時はランダムな組に分けてマップ各所へ分散スポーンさせるようにしました（固定メンバーでの巡回を防止。組分けは開票後に投票パネルの演出で発表されます）
- 分散スポーン直後に死亡が起きた場合、カウントダウン無しで即座に会議が始まるようにしました（この間は画面上部の残り時間の下に「引き継ぎ期間中…」と表示されます）
- 会議チャットログに「Taxman」のシステムメッセージを追加しました。ここまでの経過と前回の組分けを、ログ上部の固定枠ではなくチャットの流れの中でお知らせします
- 会議チャットに出る破壊された貴重品の額を、試合開始からの累計ではなく前回の会議からの増減に変更しました
- 会議チャットログの発言に新着の着信音を追加しました
- ラウンド残り5分の合図に専用のベルの音を追加しました
- ゲーム内説明書に「会議後の散開」のページを追加しました
- 全参加者へ試合ごとの識別番号（1〜N）を割り当て、役職公開画面・頭上・投票パネル・会議チャットログ・試合結果画面へ表示するようにしました（名前を知らない相手も「3番」と呼べます）
- 試合中の見た目（コスメ）変更を既定でブロックするようにしました
- 試合結果画面のアバターに、会議の投票パネルと同じ死因アイコン（死亡・処刑・回線落ち）を表示するようにしました
- 配信者向けセーフモードを追加しました。一部のパロディ表現や特徴的な効果音を汎用素材・無音へ置き換えます（設定方法はREADME「配信者向けセーフモード」参照）
- 前の試合の結果画面のロビー帰還タイマーが次の試合へ持ち越され、開始直後に全員が突然ロビーへ戻されることがある不具合を修正しました
- 改造クライアントが偽の試合終了・死亡発表・フェーズ変更を全員へ送りつけ、偽の結果画面などで進行を妨害できる問題を修正しました

### English

- Added no contest. When a bug, a disconnect, or an accident makes the match impossible to continue, the host can end it by holding `VoidMatchKey` (F5 by default), then holding it again to confirm. No team wins and no cosmetic tokens are awarded, but the result screen and the match log are shown as usual
- When a meeting ends and enough survivors are left, they are now split into random groups and scatter-spawned across separate parts of the map (prevents fixed patrol groups; the group assignment is revealed in the vote panel once the votes are in)
- If someone dies right after a scatter spawn, a meeting now starts immediately, with no countdown (meanwhile, "Handover in progress…" is shown below the remaining time at the top of the screen)
- The meeting chat log now carries system messages from "Taxman". The recap of what happened so far and the previous group assignment now arrive in the chat flow instead of the pinned box at the top of the log
- The recap now reports the value of destroyed valuables as the change since the previous meeting instead of the running total for the match
- Chat messages in the meeting chat log now play a notification sound as they arrive
- The five-minute mark of the round timer now rings a dedicated bell
- Added a "Scattering After Meetings" page to the in-game manual
- Each participant is now assigned an ID number (1-N) for the match, shown on the role reveal screen, overhead, and on the vote panel, meeting chat log, and match result screen (so you can call someone "No. 3" without knowing their name)
- Changing your appearance (cosmetics) during a match is now blocked by default
- The match result screen now overlays the same death-cause icons (dead / executed / disconnected) on avatars as the meeting vote panel
- Added a streamer-safe mode that replaces some parody visuals and distinctive sound effects with generic assets or silence (see "Streamer-Safe Mode" in the README for how to turn it on)
- Fixed the previous match's result-screen return timer carrying over into the next match, which could send everyone back to the lobby shortly after the start
- Fixed modified clients being able to broadcast fake game-over, death, and phase-change messages to everyone, which could disrupt a match with things like a fake result screen

## 1.0.0

### 日本語

- 最初のリリース

### English

- Initial release
