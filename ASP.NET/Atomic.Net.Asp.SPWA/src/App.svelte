<script lang="ts">
    import * as formatMsg from "format-message";
    import { apiClient } from "./httpClient";
    import { isAxiosError } from "axios";
    let fooId: number = 1;
    let getFoo: Promise<any>;

    function submit() {
        getFoo = fetchFoo(fooId);
    }

    async function fetchFoo(fooId: number): Promise<any> {
        try {
            const res = await apiClient.get(`/api/foos/${fooId}`);
            return res.data;
        } catch (error: unknown) {
            if (isAxiosError(error)) {
                throw error.response?.data ?? error;
            }
            throw error;
        }
    }

    function format(error: any): string {
        var detail = formatMsg.default(error.detail, error.args);
        return `${error.fieldName} ${detail}`;
    }
</script>

<main>
    <form on:submit|preventDefault={submit} aria-label="Foo fetch form">
        <label for="foo-id-input">
            Foo ID:
            <input
                data-testid="foo-id-input"
                type="number"
                bind:value={fooId}
                min="1"
            />
        </label>
        <button type="submit" data-testid="submit-btn">Submit</button>
    </form>

    {#if getFoo}
        {#await getFoo}
            <span role="status" aria-live="polite">Loading...</span>
        {:then data}
            <div role="region" aria-label="Foo data">
                <span
                    >ID: <span data-testid="foo-result-id">{data.id}</span
                    ></span
                >
                <br />
                <span
                    >Full Name: <span data-testid="foo-result-fullname"
                        >{data.fullName}</span
                    ></span
                >
                <br />
                <span
                    >Sensitive Data: <span data-testid="foo-result-sensitive"
                        >{data.sensitiveData}</span
                    ></span
                >
            </div>
        {:catch error}
            {#if error.title}
                <div data-testid="validation-errors">
                    <span>{error.title}</span><br />
                    <div>
                        {#each error.errors as errorDetail, n}
                            <span data-testid="validation-errors-details-{n}">
                                {format(errorDetail)}
                            </span>
                        {/each}
                    </div>
                </div>
            {:else if error.message}
                <div role="alert">
                    <span data-testid="server-errors">
                        {error.message}
                    </span>
                </div>
            {/if}
        {/await}
    {/if}
</main>
