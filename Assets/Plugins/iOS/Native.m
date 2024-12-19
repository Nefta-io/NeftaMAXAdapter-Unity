#import <Foundation/Foundation.h>
#import <AppLovinSDK/AppLovinSDK.h>
#import <NeftaSDK/NeftaSDK-Swift.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>

#ifdef __cplusplus
extern "C" {
#endif
    void CheckTrackingPermission();
#ifdef __cplusplus
}
#endif

void CheckTrackingPermission() {
    __block BOOL canTrack = NO;
    if (@available(iOS 14.5, *)) {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            [ALPrivacySettings setHasUserConsent: status == ATTrackingManagerAuthorizationStatusAuthorized];
            [NeftaPlugin._instance SetTrackingWithIsAuthorized: status == ATTrackingManagerAuthorizationStatusAuthorized];
        }];
    } else {
        [ALPrivacySettings setHasUserConsent: canTrack];
        [NeftaPlugin._instance SetTrackingWithIsAuthorized: canTrack];
    }
}