# Changelog

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
