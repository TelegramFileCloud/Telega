using System.Threading.Tasks;
using LanguageExt;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Messages;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Messages;
using Telega.Utils;
using static LanguageExt.Prelude;

namespace Telega
{
    public sealed class TelegramClientMessages
    {
        readonly TgBellhop _tg;
        internal TelegramClientMessages(Some<TgBellhop> tg) => _tg = tg;


        public async Task<Dialogs> GetDialogs() =>
            await _tg.Call(new GetDialogs(
                offsetDate: 0,
                offsetPeer: new InputPeer.SelfTag(),
                limit: 100,
                excludePinned: false,
                offsetId: 0,
                hash: 0,
                folderId: None
            ));

        public async Task<Messages> GetHistory(
            Some<InputPeer> peer,
            int offsetId = 0,
            int offsetDate = 0,
            int addOffset = 0,
            int limit = 100,
            int maxId = 0,
            int minId = 0,
            int hash = 0
        ) =>
            await _tg.Call(new GetHistory(
                peer,
                offsetId,
                offsetDate,
                addOffset,
                limit,
                maxId,
                minId,
                hash
            ));

        public async Task<UpdatesType> SendMessage(Some<InputPeer> peer, Some<string> message) =>
            await _tg.Call(new SendMessage(
                peer: peer,
                message: message,
                randomId: Rnd.NextInt64(),
                noWebpage: true,
                silent: false,
                background: false,
                clearDraft: false,
                replyToMsgId: None,
                replyMarkup: None,
                entities: None,
                scheduleDate: None
            ));

        public async Task<UpdatesType> SendPhoto(
            Some<InputPeer> peer,
            Some<InputFile> file,
            Some<string> message
        ) =>
            await _tg.Call(new SendMedia(
                randomId: Rnd.NextInt64(),
                background: false,
                clearDraft: false,
                media: new InputMedia.UploadedPhotoTag(file: file, stickers: None, ttlSeconds: None),
                peer: peer,
                entities: None,
                replyToMsgId: None,
                replyMarkup: None,
                message: message,
                silent: false,
                scheduleDate: None
            ));

        public async Task<UpdatesType> SendDocument(
            Some<InputPeer> peer,
            Some<InputFile> file,
            Some<string> mimeType,
            Arr<DocumentAttribute> attributes,
            Some<string> message
        ) =>
            await _tg.Call(new SendMedia(
                randomId: Rnd.NextInt64(),
                background: false,
                clearDraft: false,
                media: new InputMedia.UploadedDocumentTag(
                    nosoundVideo: false,
                    file: file,
                    mimeType: mimeType,
                    attributes: attributes,
                    thumb: None,
                    stickers: None,
                    ttlSeconds: None
                ),
                peer: peer,
                silent: false,
                replyToMsgId: None,
                replyMarkup: None,
                entities: None,
                message: message,
                scheduleDate: None
            ));

        public async Task<bool> SendTyping(Some<InputPeer> peer) =>
            await _tg.Call(new SetTyping(
                action: new SendMessageAction.TypingTag(),
                peer: peer
            ));

        public async Task<MessageMedia> UploadMediaAsPhoto(Some<InputPeer> peer,
            Some<InputFile> file,
            Option<Arr<InputDocument>> stickers,
            Option<int> ttlSeconds) =>
            await _tg.Call(func: new UploadMedia(
                peer: peer,
                media: new InputMedia.UploadedPhotoTag(
                    file: file,
                    stickers: stickers,
                    ttlSeconds: ttlSeconds
                )));

        
    }
}
