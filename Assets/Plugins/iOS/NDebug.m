#import <Foundation/Foundation.h>
#import <UnityFramework/UnityFramework-Swift.h>

#ifdef __cplusplus
extern "C" {
#endif
    UIViewController* UnityGetGLViewController();

    typedef void (*OnCallback)();

    void NDebug_Open(const char *title, OnCallback onShow, OnCallback onClick, OnCallback onReward, OnCallback onClose);
#ifdef __cplusplus
}
#endif

void NDebug_Open(const char *title, OnCallback onShow, OnCallback onClick, OnCallback onReward, OnCallback onClose) {
    NSString *t = title ? [NSString stringWithUTF8String: title] : nil;
    UIViewController *viewController = UnityGetGLViewController();

    [NDebug OpenWithTitle: t
           viewController: viewController
                   onShow: ^void() { onShow(); }
                  onClick: ^void() { onClick(); }
                  onClose: ^void() { onClose(); }
                 onReward: ^void() { if (onReward != nil) { onReward(); } }];
}
