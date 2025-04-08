//
//  NeftaAdapter.m
//  UnityFramework
//
//  Created by Tomaz Treven on 18/11/2023.
//

#import <Foundation/Foundation.h>
#import <WebKit/WebKit.h>
#import <NeftaSDK/NeftaSDK-Swift.h>
#import <ALNeftaMediationAdapter.h>

#ifdef __cplusplus
extern "C" {
#endif
    typedef void (*OnBehaviourInsight)(const char *behaviourInsight);

    void EnableLogging(bool enable);
    void NeftaPlugin_Init(const char *appId, OnBehaviourInsight onBehaviourInsight);
    void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload);
    void NeftaPlugin_OnExternalMediationRequest(int adType, const char *recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, const char *adUnitId, double revenue, const char *precision, int status);
    void NeftaPlugin_OnExternalMediationImpressionAsString(const char *network, const char *format, const char *creativeId, const char *data);
    const char * NeftaPlugin_GetNuid(void *instance, bool present);
    void NeftaPlugin_SetContentRating(const char *rating);
    void NeftaPlugin_GetBehaviourInsight(const char *insights);
    void NeftaPlugin_SetOverride(const char *root);
#ifdef __cplusplus
}
#endif

NeftaPlugin *_plugin;

void NeftaPlugin_EnableLogging(bool enable) {
    [NeftaPlugin EnableLogging: enable];
}

void NeftaPlugin_Init(const char *appId, OnBehaviourInsight onBehaviourInsight) {
    _plugin = [NeftaPlugin InitWithAppId: [NSString stringWithUTF8String: appId]];
    _plugin.OnBehaviourInsightAsString = ^void(NSString * _Nonnull behaviourInsight) {
        const char *cBI = [behaviourInsight UTF8String];
        onBehaviourInsight(cBI);
    };
}

void NeftaPlugin_Record(int type, int category, int subCategory, const char *name, long value, const char *customPayload) {
    NSString *n = name ? [NSString stringWithUTF8String: name] : nil;
    NSString *cp = customPayload ? [NSString stringWithUTF8String: customPayload] : nil;
    [_plugin RecordWithType: type category: category subCategory: subCategory name: n value: value customPayload: cp];
}

void NeftaPlugin_OnExternalMediationRequest(int adType, const char *recommendedAdUnitId, double requestedFloorPrice, double calculatedFloorPrice, const char *adUnitId, double revenue, const char *precision, int status) {
    NSString *r = recommendedAdUnitId ? [NSString stringWithUTF8String: recommendedAdUnitId] : nil;
    NSString *a = adUnitId ? [NSString stringWithUTF8String: adUnitId] : nil;
    NSString *p = precision ? [NSString stringWithUTF8String: precision] : nil;
    [NeftaPlugin OnExternalMediationRequest: @"max" adType: adType recommendedAdUnitId: r requestedFloorPrice: requestedFloorPrice calculatedFloorPrice: calculatedFloorPrice adUnitId: a revenue: revenue precision: p status: status];
}

void NeftaPlugin_OnExternalMediationImpressionAsString(const char *network, const char *format, const char *creativeId, const char *data) {
    NSString *n = network ? [NSString stringWithUTF8String: network] : nil;
    NSString *f = format ? [NSString stringWithUTF8String: format] : nil;
    NSString *c = creativeId ? [NSString stringWithUTF8String: creativeId] : nil;
    NSString *d = data ? [NSString stringWithUTF8String: data] : nil;
    [ALNeftaMediationAdapter OnExternalMediationImpressionAsString: n format: f creativeId: c data: d];
}

void NeftaPlugin_SetContentRating(const char *rating) {
    [_plugin SetContentRatingWithRating: [NSString stringWithUTF8String: rating]];
}

const char * NeftaPlugin_GetNuid(void *instance, bool present) {
    const char *string = [[_plugin GetNuidWithPresent: present] UTF8String];
    char *returnString = (char *)malloc(strlen(string) + 1);
    strcpy(returnString, string);
    return returnString;
}

void NeftaPlugin_GetBehaviourInsight(const char *insights) {
    [_plugin GetBehaviourInsightWithString: [NSString stringWithUTF8String: insights]];
}

void NeftaPlugin_SetOverride(const char *root) {
    [NeftaPlugin SetOverrideWithUrl: [NSString stringWithUTF8String: root]];
}