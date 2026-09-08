export interface SignInRequest {
    emailAddress: string;
    password: string;
}

export interface SignInOrganisation {
    organisationId: string;
    organisationMembershipId: string;
    organisationCode: string;
    organisationName: string;
    organisationSlug: string;
    isPrimaryOrganisation: boolean;
    roles: string[];
    permissions: string[];
}

export interface SignInResponse {
    succeeded: boolean;
    resultCode: string;
    message: string | null;

    userAccountId: string | null;
    displayName: string | null;

    isPlatformUser: boolean;
    requiresMfa: boolean;
    mustChangePassword: boolean;

    accessToken: string | null;
    tokenType: string | null;
    expiresDateTime: string | null;

    organisations: SignInOrganisation[];
}