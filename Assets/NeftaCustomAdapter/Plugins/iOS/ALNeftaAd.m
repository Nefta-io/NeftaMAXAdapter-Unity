//
//  ALNeftaAd.m
//  MaxAdapter
//
//  Created by Tomaz Treven on 3. 10. 24.
//

#import "ALNeftaAd.h"

@implementation ALNeftaAd
- (void) SetCustomParameterWithProvider:(NSString *)provider value: (NSString *)value {
}
-(void) Load {
}
-(int) CanShow {
    return 0;
}
-(void) Show:(UIViewController *)viewController {
}
-(void) Close {
}

+(MAAdapterError*) NLoadToAdapterError:(NError *)error {
    if (error._code == CodeNetwork) {
        return MAAdapterError.noConnection;
    }
    if (error._code == CodeRequest) {
        return MAAdapterError.badRequest;
    }
    if (error._code == CodeTimeout) {
        return MAAdapterError.timeout;
    }
    if (error._code == CodeResponse) {
        return MAAdapterError.serverError;
    }
    if (error._code == CodeNoFill) {
        return MAAdapterError.noFill;
    }
    if (error._code == CodeExpired) {
        return MAAdapterError.adExpiredError;
    }
    if (error._code == CodeInvalidState) {
        return MAAdapterError.adNotReady;
    }
    if (error._code == CodeParse) {
        return MAAdapterError.internalError;
    }
    return MAAdapterError.unspecified;
}
@end
