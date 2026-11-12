#import <Foundation/Foundation.h>
#import <UnityFramework/UnityFramework-Swift.h>
#import <AppLovinSDK/AppLovinSDK.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

#ifdef __cplusplus
extern "C" {
#endif
    void CheckTrackingPermission();

    UIViewController* UnityGetGLViewController();

    typedef void (*OnCallback)();

    void NDebug_Open(const char *title, OnCallback onShow, OnCallback onClick, OnCallback onReward, OnCallback onClose);
#ifdef __cplusplus
}
#endif

void CheckTrackingPermission() {
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(2.0 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        if (@available(iOS 14, *)) {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            }];
        }
    });
}

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
